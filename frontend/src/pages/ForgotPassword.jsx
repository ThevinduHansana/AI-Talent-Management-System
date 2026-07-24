import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link } from 'react-router-dom';
import { FiMail, FiCheckCircle, FiArrowLeft } from 'react-icons/fi';
import { authApi } from '../api';
import { useToast } from '../contexts/ToastContext';
import { getErrorMessage } from '../api/client';
import { Spinner } from '../components/ui';
import AuthCard, { authInput, FieldIcon, FieldError } from '../components/AuthCard';

export default function ForgotPassword() {
  const { toast } = useToast();
  const [sent, setSent] = useState(false);
  const [sentEmail, setSentEmail] = useState('');
  // Dev convenience only: fetched from an endpoint that does not exist in Release builds.
  const [devToken, setDevToken] = useState(null);

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm({
    defaultValues: { email: '' },
  });

  const onSubmit = async ({ email }) => {
    try {
      await authApi.forgotPassword(email);
      setSentEmail(email);
      setSent(true);

      // In dev there is no mailer, so pull a usable token from the dev-only endpoint to keep the
      // flow clickable. Any failure (unknown email, endpoint absent) just falls back to the notice.
      if (import.meta.env.DEV) {
        try {
          const { resetToken } = await authApi.devResetToken(email);
          setDevToken(resetToken);
        } catch {
          setDevToken(null);
        }
      }
    } catch (error) {
      toast(getErrorMessage(error, 'Could not start the password reset.'), 'error');
    }
  };

  return (
    <AuthCard
      heading="Forgot password?"
      subheading={sent ? 'Check your inbox' : 'We’ll email you a reset link'}
      panelHeading="Locked out?"
      panelText="It happens. Enter the email address on your account and we’ll send you a link to choose a new password. The link stays valid for one hour."
    >
      {sent ? (
        <div className="mt-10 text-center">
          <span className="mx-auto mb-5 flex h-14 w-14 items-center justify-center rounded-full bg-gradient-to-br from-brand-600 to-brand-500 text-white shadow-[0_6px_20px_-6px_rgba(79,70,229,0.55)]">
            <FiCheckCircle className="h-6 w-6" />
          </span>
          <p className="text-sm leading-relaxed text-slate-600 dark:text-slate-300">
            If an account exists for <span className="font-semibold text-slate-900 dark:text-white">{sentEmail}</span>,
            a password reset link is on its way. It expires in one hour.
          </p>

          {import.meta.env.DEV && (
            <div className="mt-6 rounded-2xl border border-amber-300/70 bg-amber-50 p-4 text-left dark:border-amber-700/50 dark:bg-amber-950/30">
              <p className="text-xs font-semibold uppercase tracking-wider text-amber-700 dark:text-amber-400">
                Dev mode — no mailer configured
              </p>
              {devToken ? (
                <>
                  <p className="mt-1.5 text-xs text-amber-800/90 dark:text-amber-200/80">
                    Skip the email and continue straight to the reset form:
                  </p>
                  <Link
                    to={`/reset-password?email=${encodeURIComponent(sentEmail)}&token=${encodeURIComponent(devToken)}`}
                    className="mt-3 inline-block text-xs font-semibold text-brand-600 underline underline-offset-2 hover:text-brand-700 dark:text-brand-400"
                  >
                    Continue to reset password →
                  </Link>
                </>
              ) : (
                <p className="mt-1.5 text-xs text-amber-800/90 dark:text-amber-200/80">
                  No account matches that email, so no reset link was generated.
                </p>
              )}
            </div>
          )}

          <Link to="/login" className="mt-8 inline-flex items-center gap-1.5 text-sm font-semibold text-brand-600 hover:text-brand-700 dark:text-brand-400">
            <FiArrowLeft className="h-4 w-4" /> Back to sign in
          </Link>
        </div>
      ) : (
        <>
          <form onSubmit={handleSubmit(onSubmit)} className="mt-10 space-y-5" noValidate>
            <div>
              <label className="sr-only" htmlFor="email">Email</label>
              <div className="relative">
                <FieldIcon icon={FiMail} />
                <input id="email" type="email" placeholder="E-mail" className={`${authInput} pl-14 pr-4`}
                  {...register('email', {
                    required: 'Email is required',
                    pattern: { value: /^\S+@\S+$/, message: 'Enter a valid email' },
                  })} />
              </div>
              <FieldError error={errors.email} />
            </div>

            <button
              type="submit"
              className="btn-primary w-full !rounded-full py-3.5 text-sm font-semibold uppercase tracking-wider"
              disabled={isSubmitting}
            >
              {isSubmitting ? <Spinner className="h-4 w-4 text-white" /> : 'Send reset link'}
            </button>
          </form>

          <p className="mt-8 text-center text-sm text-slate-500 dark:text-slate-400">
            Remembered it?{' '}
            <Link to="/login" className="font-semibold text-brand-600 hover:text-brand-700 dark:text-brand-400">Sign in</Link>
          </p>
        </>
      )}
    </AuthCard>
  );
}
