using OpenAI_API.Chat;

namespace PowerSchnell.Web.Models
{
    public class SessionMessage
    {
        public string? Role {  get; set; }
        public string? Content { get; set; }

        public ChatMessageRole GetChatMessageRole()
        {
            switch(Role.ToLower())
            {
                case "user":
                    return ChatMessageRole.User;

                case "assistant":
                    return ChatMessageRole.Assistant;

                case "system":
                    return ChatMessageRole.System;

                default:
                    return ChatMessageRole.User;
            }
        }
    }
}
