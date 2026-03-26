/* 
Pseudocode / Plan:
1. Create a simple C# type to represent the JSON structure found in metadata.json.
2. Properties:
   - Subject : string
   - FromName : string
   - FromEmail : string
   - Date : DateTimeOffset (to preserve timezone info in the JSON)
3. Initialize string properties to empty to avoid nullability warnings.
4. Provide a short commented example showing how to deserialize and print FromName and Date:
   - var meta = JsonSerializer.Deserialize<PrintMetadata>(metaData, options);
   - Console.WriteLine($"{meta.FromName} - {meta.Date}");
5. Keep the type in the same namespace as the WinForms project for easy use from Form1.

Add this file to the project and replace existing Deserialize<object>(...) calls with Deserialize<PrintMetadata>(...).
*/

using System;

namespace WinFormsApp3
{
    public record PrintMetadata
    {
        public string Subject { get; init; } = string.Empty;
        public string FromName { get; init; } = string.Empty;
        public string FromEmail { get; init; } = string.Empty;

        public string FromDisplay => $"{Date:hh:mm tt} - {FromName} - {FromEmail}";

        public int Id { get; init; }
        public DateTimeOffset Date { get; init; }
    }
}
    