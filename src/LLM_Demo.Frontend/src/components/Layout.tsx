import { Outlet, Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function Layout() {
  const { isAuthenticated, logout } = useAuth();

  return (
    <div className="min-h-screen flex flex-col">
      {/* Header */}
      <header className="bg-white border-b border-gray-200 shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between h-16">
            <Link to="/" className="flex items-center gap-2 text-xl font-bold text-indigo-600 hover:text-indigo-700 transition-colors">
              <span className="text-2xl">🤖</span>
              <span>LLM Demo</span>
            </Link>

            {isAuthenticated && (
              <button
                onClick={logout}
                className="btn-secondary text-sm"
              >
                Выйти
              </button>
            )}
          </div>
        </div>
      </header>

      {/* Main content */}
      <main className="flex-1">
        <Outlet />
      </main>
    </div>
  );
}
