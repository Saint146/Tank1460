namespace Tank1460.SaveLoad;

internal enum SavingState
{
    NotSaving,
    ReadyToSelectStorageDevice,
    SelectingStorageDevice,

    ReadyToOpenStorageContainer,
    OpeningStorageContainer,
    ReadyToSave
}