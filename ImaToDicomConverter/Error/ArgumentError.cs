using LanguageExt.Common;

namespace ImaToDicomConverter.Errors;

internal record ArgumentError(string Message) : Error
{
    public override bool Is<E>()
    {
        throw new NotImplementedException();
    }

    public override ErrorException ToErrorException()
    {
        throw new NotImplementedException();
    }

    public override string Message { get; } = Message;
    public override bool IsExceptional => false;
    public override bool IsExpected => true;
}