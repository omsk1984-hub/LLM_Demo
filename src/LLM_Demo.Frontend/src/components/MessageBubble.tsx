import { memo } from 'react';
import type { Message } from '../types/chat';

interface MessageBubbleProps {
  message: Pick<Message, 'role' | 'content'>;
}

const roleStyles: Record<string, string> = {
  User: 'bg-indigo-600 text-white rounded-br-sm ml-12',
  Assistant: 'bg-white border border-gray-200 rounded-bl-sm mr-12',
  System: 'bg-gray-100 text-gray-500 text-sm italic text-center mx-auto',
  Tool: 'bg-yellow-50 border border-yellow-200 text-yellow-800 text-sm font-mono mx-auto max-w-lg',
};

const roleLabels: Record<string, string> = {
  User: 'Вы',
  Assistant: 'Ассистент',
  System: 'Система',
  Tool: 'Инструмент',
};

function MessageBubble({ message }: MessageBubbleProps) {
  const style = roleStyles[message.role] ?? roleStyles.System;
  const label = roleLabels[message.role] ?? message.role;

  return (
    <div className="flex flex-col gap-1">
      {message.role !== 'User' && (
        <span className="text-xs text-gray-400 px-1">{label}</span>
      )}
      <div className={`rounded-xl px-4 py-3 ${style}`}>
        <p className="whitespace-pre-wrap break-words">{message.content}</p>
      </div>
    </div>
  );
}

export default memo(MessageBubble);
