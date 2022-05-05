﻿using System;
using System.Diagnostics.CodeAnalysis;

namespace CodeCaster.PVBridge
{
    public class ApiResponse
    {
        public ApiResponseStatus Status { get; }

        public virtual bool IsSuccessful => Status == ApiResponseStatus.Succeeded;

        /// <summary>
        /// The date when this API call can be retried.
        /// </summary>
        public DateTime? RetryAfter { get; init; }

        public static ApiResponse Succeeded => new(ApiResponseStatus.Succeeded);

        public static ApiResponse RateLimited(DateTime retryAfter) => new(ApiResponseStatus.RateLimited)
        {
            RetryAfter = retryAfter.ToUniversalTime()
        };

        public ApiResponse(ApiResponseStatus status)
        {
            Status = status;
        }
    }

    public class ApiResponse<T> : ApiResponse
    {
        /// <summary>
        /// Returns whether this response is successful and contains a response, and if it's a collection, contains any elements.
        /// </summary>
        [MemberNotNullWhen(true, nameof(Response))]
        public override bool IsSuccessful => Status == ApiResponseStatus.Succeeded
                                    && Response != null
                                    && (Response is not System.Collections.ICollection c || c.Count > 0);

        /// <summary>
        /// The result from calling the API, or something else you want to return as result.
        /// </summary>
        public T? Response { get; }

        public new static ApiResponse<T> RateLimited(DateTime retryAfter) => new(new ApiResponse(ApiResponseStatus.RateLimited));

        public static ApiResponse<T> Failed(T? response = default) => new(response, ApiResponseStatus.Failed);

        public ApiResponse(ApiResponse other)
            : this(other.Status)
        {
            RetryAfter = other.RetryAfter;
        }

        public ApiResponse(ApiResponseStatus status)
            : base(status)
        {
        }

        public ApiResponse(T? response, ApiResponseStatus status = ApiResponseStatus.Succeeded)
            : this(status)
        {
            Response = response;
        }

        public static implicit operator ApiResponse<T>(T? response)
        {
            return new(response);
        }
    }
}
