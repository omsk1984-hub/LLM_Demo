import { Link } from 'react-router-dom';

export default function NotFoundPage() {
  return (
    <div className="min-h-[calc(100vh-4rem)] flex items-center justify-center px-4">
      <div className="text-center">
        <div className="text-8xl mb-4">404</div>
        <h1 className="text-3xl font-bold text-gray-900 mb-2">
          Страница не найдена
        </h1>
        <p className="text-gray-500 mb-6">
          Такой страницы не существует. Проверьте URL или вернитесь на главную.
        </p>
        <Link to="/chat" className="btn-primary inline-block">
          Перейти к чату
        </Link>
      </div>
    </div>
  );
}
