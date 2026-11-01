
from fastapi import FastAPI, UploadFile, File, HTTPException
from fastapi.responses import JSONResponse
from insightface.app import FaceAnalysis
from pathlib import Path
import cv2
import numpy as np
import logging

# ============================================
# LOGGING
# ============================================

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# ============================================
# FASTAPI APPLICATION
# ============================================

app = FastAPI(title="Student Attendance Face Engine")

# ============================================
# INSIGHTFACE
# ============================================

face_app = FaceAnalysis(
    name="buffalo_l",
    addons=["liveness"],
    providers=["CPUExecutionProvider"]
)

face_app.prepare(
    ctx_id=0,
    det_size=(640, 640)
)

# ============================================
# FACE DATA FOLDER
# ============================================

BASE_DIR = Path(__file__).resolve().parent
FACE_FOLDER = BASE_DIR / "faces"
FACE_FOLDER.mkdir(parents=True, exist_ok=True)

THRESHOLD = 0.50

# ============================================
# READ IMAGE
# ============================================

async def read_image(file: UploadFile):

    data = await file.read()

    if not data:
        return None

    image_array = np.frombuffer(data, dtype=np.uint8)

    image = cv2.imdecode(
        image_array,
        cv2.IMREAD_COLOR
    )

    return image


# ============================================
# COSINE SIMILARITY
# ============================================

def cosine_similarity(first, second):

    first = np.asarray(first, dtype=np.float32)
    second = np.asarray(second, dtype=np.float32)

    if first.shape != second.shape:
        return 0.0

    denominator = (
        np.linalg.norm(first) *
        np.linalg.norm(second)
    )

    if denominator == 0:
        return 0.0

    return float(
        np.dot(first, second) / denominator
    )


# ============================================
# LIVENESS RESULT VALIDATION
# ============================================

def get_liveness(face):

    live_result = getattr(face, "liveness", None)

    # Liveness is missing or unavailable.
    if live_result is None:
        logger.error(
            "Liveness result is None. "
            "The liveness model did not provide a result."
        )
        return None, None

    # Support dictionary-based results.
    if isinstance(live_result, dict):

        is_live = live_result.get("is_live")
        live_score = live_result.get("live_score")

    else:

        is_live = getattr(
            live_result,
            "is_live",
            None
        )

        live_score = getattr(
            live_result,
            "live_score",
            None
        )

    # Never guess whether a face is live.
    if not isinstance(is_live, (bool, np.bool_)):
        logger.error(
            "Invalid liveness result: %r",
            live_result
        )
        return None, None

    try:
        if live_score is not None:
            live_score = float(live_score)
    except (TypeError, ValueError):
        live_score = None

    return bool(is_live), live_score


# ============================================
# LIVENESS ERROR RESPONSE
# ============================================

def liveness_unavailable():

    return JSONResponse(
        status_code=503,
        content={
            "success": False,
            "matched": False,
            "live": False,
            "message": (
                "Liveness detection is unavailable. "
                "Please check the face engine configuration."
            )
        }
    )


# ============================================
# HEALTH CHECK
# ============================================

@app.get("/")
async def root():

    return {
        "status": "running",
        "service": "Local Student Face Engine"
    }


# ============================================
# REGISTER FACE
# ============================================

@app.post("/register")
async def register(
    studentId: int,
    file: UploadFile = File(...)
):

    try:

        image = await read_image(file)

        if image is None:
            return JSONResponse(
                status_code=400,
                content={
                    "success": False,
                    "message": "Invalid or empty image."
                }
            )

        faces = face_app.get(image)

        if len(faces) == 0:
            return {
                "success": False,
                "message": "No face detected."
            }

        if len(faces) > 1:
            return {
                "success": False,
                "message": "Only one face is allowed."
            }

        face = faces[0]

        # LIVENESS CHECK
        is_live, live_score = get_liveness(face)

        if is_live is None:
            return liveness_unavailable()

        if not is_live:
            return {
                "success": False,
                "live": False,
                "liveScore": live_score,
                "message": "Liveness check failed."
            }

        # SAVE FACE EMBEDDING
        embedding = np.asarray(
            face.embedding,
            dtype=np.float32
        )

        if embedding.size == 0:
            raise ValueError("Face embedding is empty.")

        path = FACE_FOLDER / f"{studentId}.npy"

        np.save(path, embedding)

        logger.info(
            "Face registered for student ID %s",
            studentId
        )

        return {
            "success": True,
            "live": True,
            "studentId": studentId,
            "message": "Face registered successfully."
        }

    except Exception as error:

        logger.exception("Face registration failed")

        return JSONResponse(
            status_code=500,
            content={
                "success": False,
                "message": "Face registration failed.",
                "error": str(error)
            }
        )


# ============================================
# RECOGNIZE FACE
# ============================================

@app.post("/recognize")
async def recognize(
    file: UploadFile = File(...)
):

    try:

        image = await read_image(file)

        if image is None:
            return JSONResponse(
                status_code=400,
                content={
                    "success": False,
                    "matched": False,
                    "message": "Invalid or empty image."
                }
            )

        faces = face_app.get(image)

        if len(faces) == 0:
            return {
                "success": True,
                "matched": False,
                "message": "No face detected."
            }

        if len(faces) > 1:
            return {
                "success": True,
                "matched": False,
                "message": (
                    "Multiple faces detected. "
                    "Only one student should be "
                    "in front of the camera."
                )
            }

        face = faces[0]

        # LIVENESS CHECK
        is_live, live_score = get_liveness(face)

        if is_live is None:
            return liveness_unavailable()

        if not is_live:
            return {
                "success": True,
                "matched": False,
                "live": False,
                "liveScore": live_score,
                "message": (
                    "Liveness check failed. "
                    "Please look at the camera."
                )
            }

        # CURRENT FACE EMBEDDING
        current_embedding = np.asarray(
            face.embedding,
            dtype=np.float32
        )

        best_student_id = None
        best_score = 0.0

        # COMPARE REGISTERED FACES
        for face_file in FACE_FOLDER.glob("*.npy"):

            try:

                student_id = int(face_file.stem)

                registered_embedding = np.load(
                    face_file,
                    allow_pickle=False
                )

                score = cosine_similarity(
                    current_embedding,
                    registered_embedding
                )

                if score > best_score:

                    best_score = score
                    best_student_id = student_id

            except Exception:

                logger.exception(
                    "Could not process face file: %s",
                    face_file.name
                )

                continue

        # RECOGNITION THRESHOLD
        if (
            best_student_id is None
            or best_score < THRESHOLD
        ):

            return {
                "success": True,
                "matched": False,
                "live": True,
                "confidence": float(best_score),
                "message": "Face not recognized."
            }

        # MATCH FOUND
        logger.info(
            "Recognized student ID %s with score %.4f",
            best_student_id,
            best_score
        )

        return {
            "success": True,
            "matched": True,
            "live": True,
            "studentId": best_student_id,
            "confidence": float(best_score),
            "message": "Face recognized."
        }

    except Exception as error:

        logger.exception("Face recognition failed")

        return JSONResponse(
            status_code=500,
            content={
                "success": False,
                "matched": False,
                "message": "Face recognition failed.",
                "error": str(error)
            }
        )


# ============================================
# DELETE FACE
# ============================================

@app.delete("/delete/{student_id}")
async def delete_face(student_id: int):

    path = FACE_FOLDER / f"{student_id}.npy"

    if path.exists():

        path.unlink()

        logger.info(
            "Deleted face data for student ID %s",
            student_id
        )

        return {
            "success": True,
            "message": "Face data deleted."
        }

    return {
        "success": True,
        "message": "No face data found."
    }
