using System;
using ProjetDelta.UI;

try
{
    var app = new GameApp(new UIText());
    app.Run(Environment.GetCommandLineArgs());
}
catch (Exception ex)
{
    Console.WriteLine("Une erreur critique s'est produite :");
    Console.WriteLine(ex.ToString());
    Console.WriteLine("Appuie sur une touche pour fermer.");
    Console.ReadKey();
    Environment.Exit(1);
}
