//----------------------------------------------------
module Bundle
{
    enum OperationType
    {
        ShortMessage,
        LongMessage,
        SmallFile,
        BigFile,
        LongTime,
    };

    sequence<byte> RawBytes;

    struct OperationResult
    {
        string Message;
        RawBytes Data;
    };

    exception OperationException
    {
        string Operation;
    };

    interface Worker
    {
        OperationResult PerformAction(OperationType operation, int contentSizeMB) throws OperationException;

        ["amd"] OperationResult PerformActionEx(OperationType operation, int contentSizeMB);
    };
};