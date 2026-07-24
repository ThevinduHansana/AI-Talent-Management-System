import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { FiMail, FiLock, FiShield, FiEye, FiEyeOff, FiAlertTriangle } from 'react-icons/fi';
import { authApi } from '../api';
import { useToast } from '../contexts/ToastContext';
import { getErrorMessage } from '../api/client';
import { Spinner } from '../components/ui';
import AuthCard, { authInput, FieldIcon, FieldError } from '../components/AuthCard';

export default function ResetPassword() {
  const { toast } = useToast();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [showPassword, setShowPassword] = useState(false);

  // The reset link carries both values; email stays editable only when it is absent.
  const emailFromLink = params.get('email') || '';
  const token = params.get('token') || '';

  const { register, handleSubmit, watch, formState: { errors, isSubmitting } } = useForm({
    defaultValues: { email: emailFromLink, newPassword: '', confirmPassword: '' },
  });
  const newPassword = watch('newPassword');

  const onSubmit = async (values) => {
    try {
      await authApi.resetPassword({
        email: values.email,
        token,
        newPassword: values.newPassword,
        confirmPassword: values.confirmPassword,
      });
      toast('Password updated. Please sign in.', 'success');
      navigate('/login', { replace: true });
    } catch (error) {
      toast(getErrorMessage(error, 'This reset link is invalid or has expired.'), 'error');
    }
  };

  return (
    <AuthCard
      heading="New password"
      subheading="Choose a password you’ll remember"
      panelHeading="Almost there"
      panelText="Pick a strong password — at least 8 characters with an uppercase letter, a lowercase letter and a digit. Signing in again everywhere will be required afterwards."
    >
      {!token ? (
        <div className="mt-10 text-center">
          <span className="mx-auto mb-5 flex h-14 w-14 items-center justify-center rounded-full bg-amber-100 text-amber-600 dark:bg-amber-950/40 dark:text-amber-400">
            <FiAlertTriangle className="h-6 w-6" />
          </span>
          <p className="text-sm leading-relaxed text-slate-600 dark:text-slate-300">
            This reset link is missing its token. Request a fresh one — links expire after an hour.
          </p>
          <Link
            to="/forgot-password"
            className="btn-primary mt-8 w-full !rounded-full py-3.5 text-sm font-semibold uppercase tracking-wider"
          >
            Request a new link
          </Link>
        </div>
      ) : (
        <>
          <form onSubmit={handleSubmit(onSubmit)} className="mt-10 space-y-5" noValidate>
            <div>
              <label className="sr-only" htmlFor="email">Email</label>
              <div className="relative">
                <FieldIcon icon={FiMail} />
                <input
                  id="email"
                  type="email"
                  placeholder="E-mail"
                  readOnly={Boolean(emailFromLink)}
                  className={`${authInput} pl-14 pr-4 ${emailFromLink ? 'cursor-not-allowed opacity-70' : ''}`}
                  {...register('email', { required: 'Email is required' })}
                />
              </div>
              <FieldError error={errors.email} />
            </div>

            <div>
              <label className="sr-only" htmlFor="newPassword">New password</label>
              <div className="relative">
                <FieldIcon icon={FiLock} />
                <input
                  id="newPassword"
                  type={showPassword ? 'text' : 'password'}
                  placeholder="New password"
                  className={`${authInput} pl-14 pr-12`}
                  {...register('newPassword', {
                    required: 'Password is required',
                    minLength: { value: 8, message: 'At least 8 characters' },
                    pattern: { value: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/, message: 'Use upper, lower & a digit' },
                  })}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((s) => !s)}
                  className="absolute right-4 top-1/2 -translate-y-1/2 text-slate-400 transition-colors hover:text-brand-600"
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                >
                  {showPassword ? <FiEyeOff className="h-4 w-4" /> : <FiEye className="h-4 w-4" />}
                </button>
              </div>
              <FieldError error={errors.newPassword} />
            </div>

            <div>
              <label className="sr-only" htmlFor="confirmPassword">Confirm password</label>
              <div className="relative">
                <FieldIcon icon={FiShield} />
                <input
                  id="confirmPassword"
                  type={showPassword ? 'text' : 'password'}
                  placeholder="Confirm password"
                  className={`${authInput} pl-14 pr-4`}
                  {...register('confirmPassword', {
                    validate: (v) => v === newPassword || 'Passwords do not match',
                  })}
                />
              </div>
              <FieldError error={errors.confirmPassword} />
            </div>

            <button
              type="submit"
              className="btn-primary w-full !rounded-full py-3.5 text-sm font-semibold uppercase tracking-wider"
              disabled={isSubmitting}
            >
              {isSubmitting ? <Spinner className="h-4 w-4 text-white" /> : 'Reset password'}
            </button>
          </form>

          <p className="mt-8 text-center text-sm text-slate-500 dark:text-slate-400">
            <Link to="/login" className="font-semibold text-brand-600 hover:text-brand-700 dark:text-brand-400">Back to sign in</Link>
          </p>
        </>
      )}
    </AuthCard>
  );
}
