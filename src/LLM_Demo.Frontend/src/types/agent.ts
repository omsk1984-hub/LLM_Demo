export interface Agent {
  id: string;
  name: string;
  systemPrompt: string;
  status: AgentStatus;
  ownerId: string;
  createdAt: string;
  updatedAt: string;
}

export type AgentStatus = 'Idle' | 'Running' | 'WaitingForSubAgent' | 'Error';
