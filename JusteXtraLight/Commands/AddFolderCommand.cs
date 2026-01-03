namespace JustExtraLight.Commands;

public sealed partial class AddFolderCommand(MainPageViewModel mainPageViewModel) : CommandBase
{
    public override async void Execute(object? parameter)
    {
        await mainPageViewModel.AddFolderImages();
    }
}