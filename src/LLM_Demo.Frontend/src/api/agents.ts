import client from './client';
import type { Agent } from '../types/agent';

export async function getAllAgents(): Promise<Agent[]> {
  const response = await client.get<Agent[]>('/agents');
  return response.data;
}
