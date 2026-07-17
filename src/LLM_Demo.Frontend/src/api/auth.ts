import client from './client';
import type { LoginRequest, RegisterRequest, AuthResponse } from '../types/auth';

export async function login(data: LoginRequest): Promise<AuthResponse> {
  const response = await client.post<AuthResponse>('/auth/login', data);
  return response.data;
}

export async function register(data: RegisterRequest): Promise<AuthResponse> {
  const response = await client.post<AuthResponse>('/auth/register', data);
  return response.data;
}
