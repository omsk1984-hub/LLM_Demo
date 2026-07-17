import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { register } from '../api/auth';
import { useAuth } from '../context/AuthContext';

export default function RegisterPage() {
  const navigate = useNavigate();
  const { login: authLogin } = useAuth();

  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);

    if (!username.trim() || !email.trim() || !password.trim()) {
      setError('Заполните все поля');
      return;
    }

    if (password.length < 4) {
      setError('Пароль должен содержать минимум 4 символа');
      return;
    }

    try {
      setLoading(true);
      const response = await register({ username, email, password });
      authLogin(response);
      navigate('/chat', { replace: true });
    } catch (err: any) {
      const message = err?.response?.data?.error || 'Ошибка регистрации. Попробуйте снова.';
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
          <h1 className="text-2xl font-bold text-gray-900">Регистрация</h1>
          <p className="text-gray-500 text-sm mt-1">
            Создайте аккаунт для работы с AI-агентами
          </p>
        </div>

        {error && (
          <div className="bg-red-50 border border-red-200 text-red-600 rounded-lg px-4 py-3 text-sm mb-4">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label htmlFor="username" className="block text-sm font-medium text-gray-700 mb-1">
              Имя пользователя
            </label>
            <input
              id="username"
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              className="input-field"
              placeholder="username"
              required
              autoFocus
            />
          </div>

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
              minLength={4}
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            className="btn-primary w-full py-2.5"
          >
            {loading ? 'Регистрация...' : 'Зарегистрироваться'}
          </button>
        </form>

        <p className="text-center text-sm text-gray-500 mt-4">
          Уже есть аккаунт?{' '}
          <Link to="/login" className="text-indigo-600 hover:text-indigo-700 font-medium">
            Войти
          </Link>
        </p>
      </div>
    </div>
  );
}
