import { useState, type FormEvent, type KeyboardEvent } from 'react';

interface ChatInputProps {
  onSend: (message: string) => void;
  disabled: boolean;
  placeholder?: string;
}

export default function ChatInput({
  onSend,
  disabled,
  placeholder = 'Напишите сообщение...',
}: ChatInputProps) {
  const [text, setText] = useState('');

  function handleSubmit(e?: FormEvent) {
    e?.preventDefault();
    const trimmed = text.trim();
    if (!trimmed || disabled) return;
    onSend(trimmed);
    setText('');
  }

  function handleKeyDown(e: KeyboardEvent<HTMLTextAreaElement>) {
    // Enter без Shift отправляет сообщение
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit();
    }
  }

  return (
    <form onSubmit={handleSubmit} className="border-t border-gray-200 bg-white p-4">
      <div className="flex gap-3 items-end">
        <textarea
          value={text}
          onChange={(e) => setText(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder={placeholder}
          disabled={disabled}
          rows={2}
          className="input-field resize-none flex-1"
        />
        <button
          type="submit"
          disabled={disabled || !text.trim()}
          className="btn-primary px-6 py-2.5"
        >
          Отправить
        </button>
      </div>
      <p className="text-xs text-gray-400 mt-1 ml-1">
        Enter — отправить · Shift+Enter — новая строка
      </p>
    </form>
  );
}
