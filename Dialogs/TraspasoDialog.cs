using Microsoft.Bot.Builder.Dialogs;
using System.Threading.Tasks;
using System.Threading;
using System;
using Bot.Api.Data;
using Microsoft.Bot.Builder;

namespace Bot.Api.Dialogs
{
    public class TraspasoDialog : ComponentDialog
    {
        public TraspasoDialog() : base(nameof(TraspasoDialog))
        {
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ShowUsers,
                End
            }));
        }
        private async Task<DialogTurnResult> ShowUsers(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //Creamos la instancia para la conexion 
            var db = new DatabaseService("sqlserverdac.database.windows.net", "databaseac", "usrteam1", "XW9ZEzoa");
            
            // Definimos la query
            var query = "SELECT * FROM users";

            try
            {
                //Ejecutamos la conexion
                var reader = db.ExecuteReader(query);

                while (reader.Read())
                {
                    var userID = reader["UserID"];
                    var name = reader["name"];
                    var CeCo = reader["CeCo"];
                    var saldoPresupuestal = reader["SaldoPresupuestal"];

                    await stepContext.Context.SendActivityAsync($"UserID: {userID}, Name: {name}, CeCo: {CeCo}, SaldoPresupuestal: {saldoPresupuestal}");
                }

                reader.Close();
            }
            catch (Exception ex)
            {
                await stepContext.Context.SendActivityAsync("The bot encountered an error or bug.");
                await stepContext.Context.SendActivityAsync("To continue to run this bot, please fix the bot source code.");
                await stepContext.Context.SendActivityAsync(ex.Message);
            }


            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }

        private Task<DialogTurnResult> End(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
