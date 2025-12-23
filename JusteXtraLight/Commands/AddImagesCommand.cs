namespace JustExtraLight.Commands;

public sealed partial class AddImagesCommand(MainPageViewModel mainPageViewModel) : CommandBase
{
    public override async void Execute(object? parameter)
    {
        await mainPageViewModel.AddImages();
    }
}