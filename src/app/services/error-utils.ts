/**
 * Utility to safely extract error messages from various error types.
 * Handles HttpErrorResponse, Error objects, and plain objects.
 */
export function getErrorMessage(error: unknown): string {
  // If error is an HttpErrorResponse or has an error property with a message
  if (error && typeof error === 'object' && 'error' in error) {
    const httpError = error as { error: unknown };
    if (
      typeof httpError.error === 'object' &&
      httpError.error !== null &&
      'message' in httpError.error
    ) {
      const message = (httpError.error as { message: unknown }).message;
      if (typeof message === 'string') {
        return message;
      }
    }
  }

  // If error is a plain object with a message property
  if (error && typeof error === 'object' && 'message' in error) {
    const message = (error as { message: unknown }).message;
    if (typeof message === 'string') {
      return message;
    }
  }

  // If error is an Error object
  if (error instanceof Error) {
    return error.message;
  }

  // If error is a string
  if (typeof error === 'string') {
    return error;
  }

  // Fallback for unknown error types
  return 'An unexpected error occurred. Please try again.';
}
