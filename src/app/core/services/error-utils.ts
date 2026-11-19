export function getErrorMessage(error: unknown): string {
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

  if (error && typeof error === 'object' && 'message' in error) {
    const message = (error as { message: unknown }).message;
    if (typeof message === 'string') {
      return message;
    }
  }

  if (error instanceof Error) {
    return error.message;
  }

  if (typeof error === 'string') {
    return error;
  }

  return 'An unexpected error occurred. Please try again.';
}
