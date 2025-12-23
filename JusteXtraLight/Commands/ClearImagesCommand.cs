namespace JustExtraLight.Commands;

public sealed partial class ClearImagesCommand(MainPageViewModel mainPageViewModel) : CommandBase
{
    public override async void Execute(object? parameter)
    {
        await mainPageViewModel.DeleteFilesAfterConversion();
    }
}