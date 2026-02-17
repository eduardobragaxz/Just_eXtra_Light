namespace JustExtraLight.Commands;

public sealed partial class ClearImagesCommand(MainPageViewModel mainPageViewModel) : CommandBase
{
    public override void Execute(object? parameter)
    {
        mainPageViewModel.DeleteFilesAfterConversion();
    }
}