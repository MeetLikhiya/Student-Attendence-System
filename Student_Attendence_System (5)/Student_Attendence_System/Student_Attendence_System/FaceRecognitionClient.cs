using System.Net.Http.Headers;
using System.Text.Json;

namespace Student_Attendence_System
{
    public class FaceRecognitionClient
    {
        private readonly HttpClient _httpClient;

        private const string FaceEngineUrl =
            "http://127.0.0.1:8001";


        public FaceRecognitionClient(
            HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        // ==========================================
        // CHECK WHETHER FACE ALREADY EXISTS
        // ==========================================

        public async Task<int?> FindMatchingStudentAsync(
            byte[] imageBytes)
        {
            using var form =
                new MultipartFormDataContent();

            using var imageContent =
                new ByteArrayContent(imageBytes);

            imageContent.Headers.ContentType =
                new MediaTypeHeaderValue(
                    "image/jpeg");

            form.Add(
                imageContent,
                "file",
                "face-check.jpg");


            var response =
                await _httpClient.PostAsync(
                    $"{FaceEngineUrl}/recognize",
                    form);


            var json =
                await response.Content
                    .ReadAsStringAsync();


            Console.WriteLine(
                "=================================");

            Console.WriteLine(
                "FACE DUPLICATE CHECK");

            Console.WriteLine(
                $"HTTP Status: {response.StatusCode}");

            Console.WriteLine(
                $"Response: {json}");

            Console.WriteLine(
                "=================================");


            // Recognition service error
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Face engine returned " +
                    $"{response.StatusCode}: {json}");
            }


            using var document =
                JsonDocument.Parse(json);

            var root =
                document.RootElement;


            // success = false means no usable match
            if (root.TryGetProperty(
                    "success",
                    out var successElement) &&
                !successElement.GetBoolean())
            {
                return null;
            }


            // matched = false means face is not
            // currently registered
            if (root.TryGetProperty(
                    "matched",
                    out var matchedElement) &&
                !matchedElement.GetBoolean())
            {
                return null;
            }


            // No studentId means there is no match
            if (!root.TryGetProperty(
                    "studentId",
                    out var studentIdElement))
            {
                return null;
            }


            return studentIdElement.GetInt32();
        }


        // ==========================================
        // REGISTER FACE
        // ==========================================

        public async Task<bool> RegisterAsync(
            int studentId,
            byte[] imageBytes)
        {
            using var form =
                new MultipartFormDataContent();

            using var imageContent =
                new ByteArrayContent(imageBytes);

            imageContent.Headers.ContentType =
                new MediaTypeHeaderValue(
                    "image/jpeg");

            form.Add(
                imageContent,
                "file",
                "student-face.jpg");


            var response =
                await _httpClient.PostAsync(
                    $"{FaceEngineUrl}/register?studentId={studentId}",
                    form);


            var json =
                await response.Content
                    .ReadAsStringAsync();


            Console.WriteLine(
                "=================================");

            Console.WriteLine(
                "FACE ENGINE REGISTER");

            Console.WriteLine(
                $"HTTP Status: {response.StatusCode}");

            Console.WriteLine(
                $"Response: {json}");

            Console.WriteLine(
                "=================================");


            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Face engine returned " +
                    $"{response.StatusCode}: {json}");
            }


            using var document =
                JsonDocument.Parse(json);

            var root =
                document.RootElement;


            bool success =
                root.GetProperty("success")
                    .GetBoolean();


            if (!success)
            {
                string reason =
                    "Unknown reason";


                if (root.TryGetProperty(
                        "message",
                        out var message))
                {
                    reason =
                        message.GetString()
                        ?? reason;
                }
                else if (root.TryGetProperty(
                             "error",
                             out var error))
                {
                    reason =
                        error.GetString()
                        ?? reason;
                }


                throw new Exception(
                    $"Face engine rejected registration: {reason}");
            }


            return true;
        }


        // ==========================================
        // RECOGNIZE FACE
        // ==========================================

        public async Task<int?> RecognizeAsync(
            byte[] imageBytes)
        {
            using var form =
                new MultipartFormDataContent();

            using var imageContent =
                new ByteArrayContent(imageBytes);

            imageContent.Headers.ContentType =
                new MediaTypeHeaderValue(
                    "image/jpeg");

            form.Add(
                imageContent,
                "file",
                "attendance-face.jpg");


            var response =
                await _httpClient.PostAsync(
                    $"{FaceEngineUrl}/recognize",
                    form);


            var json =
                await response.Content
                    .ReadAsStringAsync();


            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Face engine returned " +
                    $"{response.StatusCode}: {json}");
            }


            using var document =
                JsonDocument.Parse(json);

            var root =
                document.RootElement;


            bool success =
                root.GetProperty("success")
                    .GetBoolean();


            if (!success)
            {
                return null;
            }


            bool matched =
                root.GetProperty("matched")
                    .GetBoolean();


            if (!matched)
            {
                return null;
            }


            return root
                .GetProperty("studentId")
                .GetInt32();
        }


        // ==========================================
        // DELETE FACE
        // ==========================================

        public async Task DeleteAsync(
            int studentId)
        {
            var response =
                await _httpClient.DeleteAsync(
                    $"{FaceEngineUrl}/delete/{studentId}");


            if (!response.IsSuccessStatusCode)
            {
                var json =
                    await response.Content
                        .ReadAsStringAsync();

                throw new Exception(
                    $"Unable to delete face: {json}");
            }
        }
    }
}