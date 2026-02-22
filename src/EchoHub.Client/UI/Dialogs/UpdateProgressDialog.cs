using Terminal.Gui.App;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;

namespace EchoHub.Client.UI.Dialogs;

public sealed class UpdateProgressDialog
{
    private readonly Dialog _dialog;
    private readonly ProgressBar _progressBar;
    private readonly Label _infoLabel;
    private readonly IApplication _app;

    public UpdateProgressDialog(IApplication app, string newVersion)
    {
        _app = app;

        _dialog = new Dialog { Title = $"Updating to {newVersion}", Width = 50, Height = 10 };

        _infoLabel = new Label
        {
            Text = "Preparing update...",
            X = 1,
            Y = 1,
            Width = Dim.Fill(2)
        };

        _progressBar = new ProgressBar
        {
            X = 1,
            Y = 3,
            Width = Dim.Fill(2),
            Fraction = 0f
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            X = Pos.Center(),
            Y = 6
        };

        _dialog.Add(_infoLabel, _progressBar);
    }

    public void UpdateProgress(float fraction, string statusText)
    {
        _progressBar.Fraction = fraction;
        _infoLabel.Text = statusText;
    }

    public void Show()
    {
        _app.Run(_dialog);
    }

    public void Close()
    {
        _app.RequestStop();
    }
}
