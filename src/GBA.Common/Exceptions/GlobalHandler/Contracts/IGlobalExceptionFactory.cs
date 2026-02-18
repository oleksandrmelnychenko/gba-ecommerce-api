namespace GBA.Common.Exceptions.GlobalHandler.Contracts;

public interface IGlobalExceptionFactory {
    IGlobalExceptionHandler New();
}