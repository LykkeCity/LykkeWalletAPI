using System;
using Lykke.Service.ClientDialogs.Client.Models;

namespace LykkeApi2.Models.Dialogs
{
    public class ClientDialogResponseModel
    {
        public string Id { get; set; }

        public DialogType Type { get; set; }

        public DialogConditionType? ConditionType { get; set; }

        public string Header { get; set; }

        public string Text { get; set; }

        public DialogActionModel[] Actions { get; set; } = Array.Empty<DialogActionModel>();
    }

    public static class ClientDialogResponseModelHelper
    {
        public static ClientDialogResponseModel ToApiModel(this ClientDialogModel model)
        {
            return new ClientDialogResponseModel
            {
                Id = model.Id,
                Type = model.Type,
                ConditionType = model.ConditionType,
                Header = model.Header,
                Text = model.Text,
                Actions = model.Actions
            };
        }
    }
}
