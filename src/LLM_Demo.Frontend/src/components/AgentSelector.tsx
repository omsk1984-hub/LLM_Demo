import { useState, useEffect } from 'react';
import { getAllAgents } from '../api/agents';
import type { Agent } from '../types/agent';

interface AgentSelectorProps {
  selectedAgentId: string | null;
  onSelect: (agentId: string) => void;
}

export default function AgentSelector({
  selectedAgentId,
  onSelect,
}: AgentSelectorProps) {
  const [agents, setAgents] = useState<Agent[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      try {
        setLoading(true);
        const data = await getAllAgents();
        if (!cancelled) {
          setAgents(data);
          setError(null);
        }
      } catch (err) {
        if (!cancelled) {
          setError('Не удалось загрузить агентов');
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    load();
    return () => { cancelled = true; };
  }, []);

  if (loading) {
    return (
      <div className="card animate-pulse">
        <div className="h-4 bg-gray-200 rounded w-3/4 mb-2" />
        <div className="h-4 bg-gray-200 rounded w-1/2" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="card bg-red-50 border-red-200">
        <p className="text-red-600 text-sm">{error}</p>
      </div>
    );
  }

  if (agents.length === 0) {
    return (
      <div className="card">
        <p className="text-gray-500 text-sm">Нет доступных агентов</p>
      </div>
    );
  }

  return (
    <div className="card">
      <h2 className="text-sm font-semibold text-gray-500 uppercase tracking-wider mb-3">
        Выберите агента
      </h2>
      <div className="space-y-2">
        {agents.map((agent) => (
          <button
            key={agent.id}
            onClick={() => onSelect(agent.id)}
            className={`w-full text-left px-4 py-3 rounded-lg transition-colors duration-150 border ${
              selectedAgentId === agent.id
                ? 'bg-indigo-50 border-indigo-300 text-indigo-700'
                : 'bg-white border-gray-200 hover:bg-gray-50 text-gray-700'
            }`}
          >
            <div className="font-medium">{agent.name}</div>
            {agent.systemPrompt && (
              <div className="text-xs text-gray-400 mt-1 line-clamp-1">
                {agent.systemPrompt}
              </div>
            )}
          </button>
        ))}
      </div>
    </div>
  );
}
