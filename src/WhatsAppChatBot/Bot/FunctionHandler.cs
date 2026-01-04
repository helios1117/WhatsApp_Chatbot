using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using WhatsAppChatBot.Common;

namespace WhatsAppChatBot.Bot;

public class FunctionHandler : IUnifiedAction
{
    public Task<string> PerformAsync()
    {
        var dict = new Dictionary<string, string>
        {
            ["file"] = "src/WhatsAppChatBot/Bot/FunctionHandler.cs",
            ["class"] = "FunctionHandler",
            ["result"] = "ok"
        };
        return Task.FromResult(JsonSerializer.Serialize(dict));
    }
}
using System.Text.Json;
using WhatsAppChatBot.Models;

namespace WhatsAppChatBot.Bot;

public interface IFunctionHandler
{
    List<Tool> GetFunctionsForOpenAI();
    string ExecuteFunction(string functionName, Dictionary<string, object> parameters, FunctionContext? context = null);
}

public class FunctionHandler : IFunctionHandler
{
    private readonly Dictionary<string, FunctionDefinitionInternal> _functions;
    private readonly ILogger<FunctionHandler> _logger;

    public FunctionHandler(ILogger<FunctionHandler> logger)
    {
        _logger = logger;
        _functions = InitializeFunctions();
    }

    public List<Tool> GetFunctionsForOpenAI()
    {
        var tools = new List<Tool>();

        foreach (var func in _functions.Values)
        {
            tools.Add(new Tool
            {
                Type = "function",
                Function = new FunctionDefinition
                {
                    Name = func.Name,
                    Description = func.Description,
                    Parameters = func.Parameters,
                    Strict = func.Strict
                }
            });
        }

        return tools;
    }

    public string ExecuteFunction(string functionName, Dictionary<string, object> parameters, FunctionContext? context = null)
    {
        try
        {
            if (!_functions.TryGetValue(functionName, out var function))
            {
                _logger.LogWarning("Unknown function called: {FunctionName}", functionName);
                return "Function not found";
            }

            _logger.LogDebug("Executing function: {FunctionName} with parameters: {Parameters}",
                functionName, JsonSerializer.Serialize(parameters));

            var result = function.Handler(parameters, context);

            _logger.LogDebug("Function {FunctionName} executed successfully", functionName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function: {FunctionName}", functionName);
            return "An error occurred while executing the function";
        }
    }

    private Dictionary<string, FunctionDefinitionInternal> InitializeFunctions()
    {
        return new Dictionary<string, FunctionDefinitionInternal>
        {
            ["getPlanPrices"] = new()
            {
                Name = "getPlanPrices",
                Description = "Get available plans and prices information available in Wassenger",
                Parameters = new { type = "object", properties = new { } },
                Handler = GetPlanPrices
            },
            ["loadUserInformation"] = new()
            {
                Name = "loadUserInformation",
                Description = "Find user name and email from the CRM",
                Parameters = new { type = "object", properties = new { } },
                Handler = LoadUserInformation
            },
            ["verifyMeetingAvailability"] = new()
            {
                Name = "verifyMeetingAvailability",
                Description = "Verify if a given date and time is available for a meeting before booking it",
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        date = new
                        {
                            type = "string",
                            format = "date-time",
                            description = "Date of the meeting"
                        }
                    },
                    required = new[] { "date" }
                },
                Handler = VerifyMeetingAvailability
            },
            ["bookSalesMeeting"] = new()
            {
                Name = "bookSalesMeeting",
                Description = "Book a sales or demo meeting with the customer on a specific date and time",
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        date = new
                        {
                            type = "string",
                            format = "date-time",
                            description = "Date of the meeting"
                        }
                    },
                    required = new[] { "date" }
                },
                Handler = BookSalesMeeting
            },
            ["currentDateAndTime"] = new()
            {
                Name = "currentDateAndTime",
                Description = "What is the current date and time",
                Parameters = new { type = "object", properties = new { } },
                Handler = CurrentDateAndTime
            }
        };
    }

    private string GetPlanPrices(Dictionary<string, object> parameters, FunctionContext? context)
    {
        return "*Send & Receive messages + API + Webhooks + Team Chat + Campaigns + CRM + Analytics*\n\n" +
               "- Platform Professional: 30,000 messages + unlimited inbound messages + 10 campaigns / month\n" +
               "- Platform Business: 60,000 messages + unlimited inbound messages + 20 campaigns / month\n" +
               "- Platform Enterprise: unlimited messages + 30 campaigns\n\n" +
               "Each plan is limited to one WhatsApp number. You can purchase multiple plans if you have multiple numbers.\n\n" +
               "*Find more information about the different plan prices and features here:*\n" +
               "https://wassenger.com/#pricing";
    }

    private string LoadUserInformation(Dictionary<string, object> parameters, FunctionContext? context)
    {
        return "I am sorry, I am not able to access the CRM at the moment. Please try again later.";
    }

    private string VerifyMeetingAvailability(Dictionary<string, object> parameters, FunctionContext? context)
    {
        if (parameters.TryGetValue("date", out var dateObj) && dateObj is string dateStr)
        {
            if (DateTime.TryParse(dateStr, out var meetingDate))
            {
                var businessHours = GetBusinessHours();
                var dayOfWeek = meetingDate.DayOfWeek.ToString().ToLower();

                if (businessHours.ContainsKey(dayOfWeek))
                {
                    return $"The requested date and time ({meetingDate:yyyy-MM-dd HH:mm}) appears to be available during our business hours. " +
                           "Please note that this is a preliminary check. Final confirmation will be provided by our team.";
                }
                else
                {
                    return "The requested date falls outside our business hours. Please choose a date during our working days (Monday to Friday).";
                }
            }
        }

        return "Please provide a valid date and time for the meeting.";
    }

    private string BookSalesMeeting(Dictionary<string, object> parameters, FunctionContext? context)
    {
        if (parameters.TryGetValue("date", out var dateObj) && dateObj is string dateStr)
        {
            if (DateTime.TryParse(dateStr, out var meetingDate))
            {
                return $"I have submitted a request to book a sales meeting for {meetingDate:yyyy-MM-dd HH:mm}. " +
                       "Our sales team will contact you shortly to confirm the meeting details and provide the meeting link. " +
                       "Please make sure to check your email for the confirmation.";
            }
        }

        return "Please provide a valid date and time to book the meeting.";
    }

    private string CurrentDateAndTime(Dictionary<string, object> parameters, FunctionContext? context)
    {
        var now = DateTime.UtcNow;
        return $"The current date and time is: {now:yyyy-MM-dd HH:mm:ss} UTC";
    }

    private static Dictionary<string, string> GetBusinessHours()
    {
        return new Dictionary<string, string>
        {
            ["monday"] = "9:00 AM - 6:00 PM",
            ["tuesday"] = "9:00 AM - 6:00 PM",
            ["wednesday"] = "9:00 AM - 6:00 PM",
            ["thursday"] = "9:00 AM - 6:00 PM",
            ["friday"] = "9:00 AM - 6:00 PM"
        };
    }
}

public class FunctionDefinitionInternal
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public object? Parameters { get; set; }
    public bool Strict { get; set; } = false;
    public Func<Dictionary<string, object>, FunctionContext?, string> Handler { get; set; } = null!;
}

public class FunctionContext
{
    public MessageData? Data { get; set; }
    public WassengerDevice? Device { get; set; }
    public List<ChatMessage>? Messages { get; set; }
}
