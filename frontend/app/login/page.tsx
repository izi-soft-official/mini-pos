export default function LoginPage() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="w-full max-w-sm bg-white border border-gray-300 rounded p-6">
        <h1 className="text-xl font-semibold text-gray-900 mb-4 text-center">Login</h1>
        <form className="flex flex-col gap-3">
          <div className="flex flex-col gap-1">
            <label className="text-sm text-gray-500" htmlFor="email">
              Email
            </label>
            <input
              id="email"
              type="email"
              className="px-2.5 py-2 border border-gray-300 rounded text-sm focus:outline-none focus:border-blue-600"
              placeholder="you@example.com"
            />
          </div>
          <div className="flex flex-col gap-1">
            <label className="text-sm text-gray-500" htmlFor="password">
              Password
            </label>
            <input
              id="password"
              type="password"
              className="px-2.5 py-2 border border-gray-300 rounded text-sm focus:outline-none focus:border-blue-600"
              placeholder="••••••••"
            />
          </div>
          <button
            type="submit"
            className="mt-2 w-full py-2.5 bg-blue-600 text-white border-none rounded text-sm font-semibold hover:bg-blue-900"
          >
            Log In
          </button>
        </form>
      </div>
    </div>
  );
}