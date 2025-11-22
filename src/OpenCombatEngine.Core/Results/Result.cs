// Copyright (c) 2025 James Duane Plotts
// Licensed under MIT License for code
// Game mechanics under OGL 1.0a
// See LEGAL.md for full disclaimers

using System;

namespace OpenCombatEngine.Core.Results
{
    /// <summary>
    /// Represents the result of an operation that can succeed or fail without throwing exceptions
    /// </summary>
    /// <typeparam name="T">The type of value returned on success</typeparam>
#pragma warning disable CA1000 // Do not declare static members on generic types
    public class Result<T>
    {
        /// <summary>
        /// Gets the value if the operation succeeded
        /// </summary>
        /// <remarks>
        /// This property should only be accessed when IsSuccess is true.
        /// Accessing it when IsSuccess is false will throw an InvalidOperationException.
        /// </remarks>
        public T Value { get; }

        /// <summary>
        /// Gets the error message if the operation failed
        /// </summary>
        /// <remarks>
        /// This property will be null or empty when IsSuccess is true.
        /// </remarks>
        public string Error { get; }

        /// <summary>
        /// Gets whether the operation succeeded
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Gets whether the operation failed
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// Initializes a new instance of the Result class
        /// </summary>
        /// <param name="value">The value on success</param>
        /// <param name="error">The error message on failure</param>
        /// <param name="isSuccess">Whether the operation succeeded</param>
        private Result(T value, string error, bool isSuccess)
        {
            if (isSuccess && !string.IsNullOrEmpty(error))
                throw new ArgumentException("Success result cannot have an error message");
            if (!isSuccess && string.IsNullOrEmpty(error))
                throw new ArgumentException("Failure result must have an error message");

            Value = value;
            Error = error;
            IsSuccess = isSuccess;
        }

        /// <summary>
        /// Creates a success result with the specified value
        /// </summary>
        /// <param name="value">The successful operation's value</param>
        /// <returns>A successful Result containing the value</returns>
        public static Result<T> Success(T value)
        {
            ArgumentNullException.ThrowIfNull(value, "Success result cannot have null value");
            
            return new Result<T>(value, string.Empty, true);
        }

        /// <summary>
        /// Creates a failure result with the specified error message
        /// </summary>
        /// <param name="error">The error message describing the failure</param>
        /// <returns>A failed Result containing the error message</returns>
        public static Result<T> Failure(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                throw new ArgumentException("Error message cannot be null or whitespace", nameof(error));
            
            return new Result<T>(default!, error, false);
        }

        /// <summary>
        /// Executes an action if the result is successful
        /// </summary>
        /// <param name="action">Action to execute with the value</param>
        /// <returns>This Result for method chaining</returns>
        public Result<T> OnSuccess(Action<T> action)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (IsSuccess)
                action(Value);
            return this;
        }

        /// <summary>
        /// Executes an action if the result is a failure
        /// </summary>
        /// <param name="action">Action to execute with the error message</param>
        /// <returns>This Result for method chaining</returns>
        public Result<T> OnFailure(Action<string> action)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (IsFailure)
                action(Error);
            return this;
        }

        /// <summary>
        /// Maps the value to a different type if successful
        /// </summary>
        /// <typeparam name="TNew">The new value type</typeparam>
        /// <param name="mapper">Function to map the value</param>
        /// <returns>A new Result with the mapped value or the same error</returns>
        public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
        {
            ArgumentNullException.ThrowIfNull(mapper);

            return IsSuccess 
                ? Result<TNew>.Success(mapper(Value)) 
                : Result<TNew>.Failure(Error);
        }

        /// <summary>
        /// Provides a string representation of the result
        /// </summary>
        /// <returns>String describing the result state</returns>
        public override string ToString()
        {
            return IsSuccess 
                ? $"Success: {Value}" 
                : $"Failure: {Error}";
        }
    }
#pragma warning restore CA1000 // Do not declare static members on generic types
}
