import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { FiMail, FiLock, FiEye, FiEyeOff } from 'react-icons/fi';
import { useAuth } from '../contexts/AuthContext';
import { useToast } from '../contexts/ToastContext';
import { getErrorMessage } from '../api/client';
import { dashboardPath } from '../utils/roles';
import { Spinner } from '../components/ui';
import AuthCard, { authInput, FieldIcon, FieldError } from '../components/AuthCard';

/** localStorage key backing the "Remember me" checkbox (email prefill only). */
const REMEMBER_KEY = 'getcareers.rememberedEmail';

export default function Login() {
  const { login } = useAuth();
  const { toast } = useToast();
  const navigate = useNavigate();
  const location = useLocation();
  const [showPassword, setShowPassword] = useState(false);

  const rememberedEmail = localStorage.getItem(REMEMBER_KEY) || '';

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm({
    defaultValues: { email: rememberedEmail, password: '', remember: Boolean(rememberedEmail) },
  });

  const onSubmit = async (values) => {
    try {
      const user = await login(values.email, values.password);
      if (values.remember) localStorage.setItem(REMEMBER_KEY, values.email);
      else localStorage.removeItem(REMEMBER_KEY);
      toast('Welcome back!', 'success');
      // Honour the originally-requested page, otherwise land on the role's dashboard.
      const from = location.state?.from?.pathname || dashboardPath(user);
      navigate(from, { replace: true });
    } catch (error) {
      toast(getErrorMessage(error, 'Invalid email or password.'), 'error');
    }
  };

  return (
    <AuthCard
      heading="Hello!"
      subheading="Sign in to your account"
      panelHeading="Welcome Back!"
      panelText="Sign in to pick up where you left off — track your applications, review candidates, and keep your hiring moving forward."
    >
      <form onSubmit={handleSubmit(onSubmit)} className="mt-10 space-y-5" noValidate>
        <div>
          <label className="sr-only" htmlFor="email">Email</label>
          <div className="relative">
            <FieldIcon icon={FiMail} />
            <input id="email" type="email" placeholder="E-mail" className={`${authInput} pl-14 pr-4`}
              {...register('email', { required: 'Email is required' })} />
          </div>
          <FieldError error={errors.email} />
        </div>

        <div>
          <label className="sr-only" htmlFor="password">Password</label>
          <div className="relative">
            <FieldIcon icon={FiLock} />
            <input id="password" type={showPassword ? 'text' : 'password'} placeholder="Password"
              className={`${authInput} pl-14 pr-12`}
              {...register('password', { required: 'Password is required' })} />
            <button
              type="button"
              onClick={() => setShowPassword((s) => !s)}
              className="absolute right-4 top-1/2 -translate-y-1/2 text-slate-400 transition-colors hover:text-brand-600"
              aria-label={showPassword ? 'Hide password' : 'Show password'}
            >
              {showPassword ? <FiEyeOff className="h-4 w-4" /> : <FiEye className="h-4 w-4" />}
            </button>
          </div>
          <FieldError error={errors.password} />
        </div>

        <div className="flex items-center justify-between px-5 text-xs">
          <label className="flex cursor-pointer items-center gap-2 text-slate-500 dark:text-slate-400">
            <input
              type="checkbox"
              className="h-3.5 w-3.5 rounded border-slate-300 text-brand-600 focus:ring-brand-500/30 dark:border-slate-600 dark:bg-slate-800"
              {...register('remember')}
            />
            Remember me
          </label>
          <Link to="/forgot-password" className="font-medium text-brand-600 hover:text-brand-700 dark:text-brand-400">
            Forgot password?
          </Link>
        </div>

        <button
          type="submit"
          className="btn-primary w-full !rounded-full py-3.5 text-sm font-semibold uppercase tracking-wider"
          disabled={isSubmitting}
        >
          {isSubmitting ? <Spinner className="h-4 w-4 text-white" /> : 'Sign in'}
        </button>
      </form>

      <p className="mt-8 text-center text-sm text-slate-500 dark:text-slate-400">
        Don&apos;t have an account?{' '}
        <Link to="/register" className="font-semibold text-brand-600 hover:text-brand-700 dark:text-brand-400">Create</Link>
      </p>
    </AuthCard>
  );
}
