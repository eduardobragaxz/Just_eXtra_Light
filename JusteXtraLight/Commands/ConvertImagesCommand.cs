namespace JustExtraLight.Commands;

public sealed partial class ConvertImagesCommand(MainPageViewModel mainPageViewModel) : CommandBase
{
    public override async void Execute(object? parameter)
    {
        await mainPageViewModel.ConvertImages();
    }
}