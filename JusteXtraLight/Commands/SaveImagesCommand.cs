namespace JustExtraLight.Commands;

public sealed partial class SaveImagesCommand(MainPageViewModel mainPageViewModel) : CommandBase
{
    public override async void Execute(object? parameter)
    {
        await mainPageViewModel.SaveImages();
    }
}