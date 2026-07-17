export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  userId: string;
  expiresAt: string;
}

export interface ErrorResponse {
  error: string;
  detail?: string;
}
