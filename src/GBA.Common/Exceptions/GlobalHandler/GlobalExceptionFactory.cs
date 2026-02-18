using GBA.Common.Exceptions.GlobalHandler.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace GBA.Common.Exceptions.GlobalHandler;

public class GlobalExceptionFactory : IGlobalExceptionFactory {
    private readonly IGlobalExceptionHandler _globalExceptionHandler;

    public GlobalExceptionFactory([FromServices] IGlobalExceptionHandler globalExceptionHandler) {
        _globalExceptionHandler = globalExceptionHandler;
    }

    public IGlobalExceptionHandler New() {
        return _globalExceptionHandler;
    }
}