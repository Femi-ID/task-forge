using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos
{
    public class Result<T>
    {
        public bool Succeeded { get; private init; }
        public T? Data { get; private init; }
        public IEnumerable<string> Errors { get; private init; } = [];

        public static Result<T> Success(T data) => new() { Succeeded = true, Data = data };
        public static Result<T> Failure(string error) => new() { Succeeded = false, Errors = [error] };
        public static Result<T> Failure(IEnumerable<string> errors) => new() { Succeeded = false, Errors = errors };
    }
}