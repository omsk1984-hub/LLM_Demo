import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { login } from '../api/auth';
import { useAuth } from '../context/AuthContext';

export default function LoginPage() {
  const navigate = useNavigate();
  const { login: authLogin } = useAuth();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);

    if (!email.trim() || !password.trim()) {
      setError('Заполните все поля');
      return;
    }

    try {
      setLoading(true);
      const response = await login({ email, password });
      authLogin(response);
      navigate('/chat', { replace: true });
    } catch (err: any) {
      const message = err?.response?.data?.error || 'Ошибка входа. Проверьте email и пароль.';
      setError(message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-h-[calc(100vh-4rem)] flex items-center justify-center px-4">
      <div className="card w-full max-w-md">
        <div className="text-center mb-6">
          <div className="text-4xl mb-2">🤖</div>
          <h1 className="text-2xl font-bold text-gray-900">Вход в систему</h1>
          <p className="text-gray-500 text-sm mt-1">
            LLM Demo — Чат с AI-агентами
          </p>
        </div>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-600 rounded-lg px-4 py-3 text-sm mb-4">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
              Email
            </label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="input-field"
              placeholder="example@mail.com"
              required
              autoFocus
            />
          </div>

          <div>
            <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1">
              Пароль
            </label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="input-field"
              placeholder="••••••••"
              required
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            className="btn-primary w-full py-2.5"
          >
            {loading ? 'Вход...' : 'Войти'}
          </button>
        </form>

        <p className="text-center text-sm text-gray-500 mt-4">
          Нет аккаунта?{' '}
          <Link to="/register" className="text-indigo-600 hover:text-indigo-700 font-medium">
            Зарегистрироваться
          </Link>
        </p>
      </div>
    </div>
  );
}
