using Microsoft.AspNetCore.Mvc;
using OpenAI_API.Completions;
using OpenAI_API;
using PowerSchnell.Web.Models;
using System.Diagnostics;
using OpenAI_API.Chat;
using Microsoft.VisualBasic;
using PowerSchnell.Wrappers;
using Microsoft.AspNetCore.SignalR.Protocol;
using OpenAI_API.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Extensions.Azure;
using System.Text;
using System.Text.RegularExpressions;

namespace PowerSchnell.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IAzureBlobStorageWrapper _azureBlobStorageWrapper;
        private readonly IHttpContextAccessor _contextAccessor;
        private Stream? EmployeeAvailability { get; set; }
        private Stream? EmployeeSkills { get; set; }
        private ChatRequest ChatRequest { get; set; }

        public HomeController(ILogger<HomeController> logger, IAzureBlobStorageWrapper azureBlobStorageWrapper, IHttpContextAccessor contextAccessor)
        {
            _logger = logger;
            _azureBlobStorageWrapper = azureBlobStorageWrapper;
            _contextAccessor = contextAccessor;

            string model = "gpt-4o";
            int maxTokens = 4095;
            if (contextAccessor.HttpContext != null)
            {
                JsonSerializerSettings options = new()
                {
                    NullValueHandling = NullValueHandling.Ignore,
                };
                var json = contextAccessor.HttpContext.Session.GetString("ChatRequest") ?? "";


                ChatRequest = new ChatRequest
                {
                    Model = model,
                    MaxTokens = maxTokens,
                    Messages = new List<ChatMessage>()
                };
                if (!string.IsNullOrEmpty(json))
                {
                    
                    List<SessionMessage> messages = JsonConvert.DeserializeObject<List<SessionMessage>>(json, options) ?? new List<SessionMessage>();
                    foreach (SessionMessage message in messages)
                    {
                        ChatRequest.Messages.Add(new ChatMessage(message.GetChatMessageRole(), message.Content));
                    }
                }
            }
        }

        public async Task<IActionResult> Index()
        {
            if (EmployeeAvailability == null)
            {
                EmployeeAvailability = await _azureBlobStorageWrapper.RetrieveFromBlob("team", "employee_availability_v3.csv");
            }
            if(EmployeeSkills == null)
            {
                EmployeeSkills = await _azureBlobStorageWrapper.RetrieveFromBlob("team", "employee_bios.csv");
            }
            string schedules = "";
            string bios = "";

            EmployeeAvailability.Position = 0;
            using (StreamReader reader = new StreamReader(EmployeeAvailability, Encoding.UTF8))
            {
                 schedules =  reader.ReadToEnd();

            }

            EmployeeSkills.Position = 0;
            using (StreamReader reader = new StreamReader(EmployeeSkills, Encoding.UTF8))
            {
                bios = reader.ReadToEnd();

            }
            ChatRequest.Messages.Add(new ChatMessage(ChatMessageRole.System, $"You are a scheduling assistant that helps find the best matches for technical consultants based on their skills and hours available.  You have a list  of CONSULTANTS with a set of skills and certifications.  You also have a list of team member SCHEDULES, with their available hours each week.  Things to note:\n \nNOTE\nWe only show weeks where the team member's available hours changes from the previous week.  For example, if you see July 1st has 24 hours, and the next record for the team member has August 5 at 40 hours, that means between July 1st and August 5, the team member was available 24 hours, moving up to 40 hours available the week of August 5.\n \nYour goal is to succinctly provide options for scheduling the right team members based on best-fit availability and skills.  Provide team member options where you provide their skills, certifications and applicable schedules.  If an option isn't an exact match of skills or availability, please summarize where there is a gap and why you made that selection.\n \nYou have a lot of schedules to parse through, so double check your work in terms of available hours and skills.\n \nCONSULTANTS:\n{bios}\n\n \nSCHEDULES:\n{schedules}.  The current date is {DateTime.Today}"));

            Store();

            return View();
        }

        public void Reset()
        {
            _contextAccessor.HttpContext.Session.Clear();
        }

        public async Task<JsonResult> SendRequest(string input)
            {
            var openAiApiKey = "sk-proj-7QmtbEUXkgyDj8e66wQvT3BlbkFJpHXTm24WgbpgUyIGkJvC"; // Replace with your OpenAI API key

            APIAuthentication aPIAuthentication = new APIAuthentication(openAiApiKey);
            OpenAIAPI openAiApi = new OpenAIAPI(aPIAuthentication);
            string response = "";

            try
            {
                if(ChatRequest.Messages.Count > 50)
                {
                    ChatRequest.Messages.Remove(ChatRequest.Messages[0]);
                }
                ChatRequest.Messages.Add(new ChatMessage(ChatMessageRole.User, input));
                var chatResult = await openAiApi.Chat.CreateChatCompletionAsync(ChatRequest);
                response = chatResult.Choices[0].Message.TextContent;
                ChatRequest.Messages.Add(new ChatMessage(ChatMessageRole.Assistant, response));
                /*
                ChatRequest.Messages.Add(new ChatMessage(ChatMessageRole.User, "Reiterate just the conclusion, including skills, certifications and show each team members availability each week based on the requested time frame."));
                chatResult = await openAiApi.Chat.CreateChatCompletionAsync(ChatRequest);
                response = chatResult.Choices[0].Message.TextContent;
                ChatRequest.Messages.Add(new ChatMessage(ChatMessageRole.Assistant, response));
                */
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Store();

            string result = "";
            foreach(var item in ChatRequest.Messages)
            {
                switch(item.Role)
                {
                    case "user":
                        result += "<b>&#129333; You:</b> " + item.TextContent + "\n\n";
                        break;

                    case "assistant":
                        result += "<b>&#128012; Powershnell:</b> " + item.TextContent + "\n\n";
                        break;
                }
            }
            return Json(AddBold(result.Replace("\n", "<br />")));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private void Store()
        {
            if (_contextAccessor.HttpContext != null)
            {
                List<SessionMessage> sessionMessages = new List<SessionMessage>();
                foreach (ChatMessage message in ChatRequest.Messages)
                {
                    sessionMessages.Add(new SessionMessage() { Role = message.Role, Content = message.TextContent });
                }
                _contextAccessor.HttpContext.Session.SetString("ChatRequest", JsonConvert.SerializeObject(sessionMessages));

            }
        }

        private string AddBold(string input)
        {
            string bold = @"\*\*(.*?)\*\*";
            string boldReplacement = @"<b>$1</b>";
            string h1 = @"###\s*(.*?)(?=\s|$)";
            string h1Replacement = @"<h1>$1</h1>";
            string h2 = @"####\s*(.*?)(?=\s|$)";
            string h2Replacement = @"<h2>$1</h2>";

            input = Regex.Replace(input, bold, boldReplacement);

            return input.Replace("####", "").Replace("###", "");
        }
    }
}
