import { Moon, Sun, LogOut } from "@/components/Icons";

export default function SettingsPage() {
  return (
    <>
      <div>
        <h1 className="text-2xl font-bold">Settings</h1>
        <p className="text-sm text-slate-500">
          Application preferences and account.
        </p>
      </div>
      <div className="bg-white border border-gray-300 rounded overflow-hidden">
        <div className="card p-6">
          <h2 className="font-bold">Language</h2>
          <select className="input mt-4 max-w-xs">
            <option value="en">English</option>
            <option value="fr">Français</option>
            <option value="ar">العربية</option>
          </select>
        </div>

        <div className="card p-6">
          <h2 className="font-bold">Appearance</h2>
          <div className="mt-4 grid gap-3 sm:grid-cols-2">
            <button className={`rounded-2xl border p-4 text-left `}>
              <Sun className="mb-2" />
              <b>Light</b>
              <p className="text-xs text-slate-500">Bright interface</p>
            </button>
            <button className={`rounded-2xl border p-4 text-left }`}>
              <Moon className="mb-2" />
              <b>Dark</b>
              <p className="text-xs text-slate-500">Dark interface</p>
            </button>
          </div>
        </div>

        <div className="card p-6">
          <h2 className="font-bold">Account</h2>
          <div className="mt-4 rounded-xl bg-slate-50 p-4 dark:bg-slate-800">
            <div className="font-semibold">User Name</div>
            <div className="text-sm capitalize text-slate-500">User Role</div>
          </div>
          <button className="px-2.5 py-1 border border-gray-300 rounded-xl bg-white text-red-700 hover:border-red-700 mt-4">
            <LogOut size={16} /> Log out
          </button>
        </div>
      </div>
    </>
  );
}
