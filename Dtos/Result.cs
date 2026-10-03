using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos
{
    public enum ResultError
    {
        None,
        NotFound,
        Forbidden,
        Conflict,
        Validation
    }
    public class Result<T>
    {
        public bool Succeeded { get; private init; }
        public T? Data { get; private init; }
        public IEnumerable<string> Errors { get; private init; } = [];
        public ResultError ErrorType { get; private init; } = ResultError.None;

        public static Result<T> Success(T data) => new() { Succeeded = true, Data = data };
        public static Result<T> Failure(string error, ResultError type = ResultError.Validation) =>
            new() { Succeeded = false, Errors = [error], ErrorType = type };
        public static Result<T> Failure(IEnumerable<string> errors, ResultError type = ResultError.Validation) =>
            new() { Succeeded = false, Errors = errors, ErrorType = type };
    }
}