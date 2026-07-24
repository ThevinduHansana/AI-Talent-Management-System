import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate } from 'react-router-dom';
import { FiUser, FiMail, FiPhone, FiLock, FiShield, FiEye, FiEyeOff } from 'react-icons/fi';
import { useAuth } from '../contexts/AuthContext';
import { useToast } from '../contexts/ToastContext';
import { getErrorMessage } from '../api/client';
import { Spinner } from '../components/ui';
import AuthCard, { authInput, FieldIcon, FieldError } from '../components/AuthCard';

export default function Register() {
  const { register: registerUser } = useAuth();
  const { toast } = useToast();
  const navigate = useNavigate();
  const [showPassword, setShowPassword] = useState(false);

  const { register, handleSubmit, watch, formState: { errors, isSubmitting } } = useForm();
  const password = watch('password');

  const onSubmit = async (values) => {
    try {
      await registerUser(values);
      toast('Account created. Welcome to GetCareers!', 'success');
      navigate('/candidate/dashboard', { replace: true });
    } catch (error) {
      toast(getErrorMessage(error, 'Registration failed.'), 'error');
    }
  };

  return (
    <AuthCard
      wide
      heading="Hello!"
      subheading="Create your candidate account"
      panelHeading="Join GetCareers"
      panelText="Upload your resume once and let AI match you to the right roles — then track every application, interview and offer in one place."
    >
      <form onSubmit={handleSubmit(onSubmit)} className="mt-10 space-y-5" noValidate>
        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
          <div>
            <label className="sr-only" htmlFor="firstName">First name</label>
            <div className="relative">
              <FieldIcon icon={FiUser} />
              <input id="firstName" placeholder="First name" className={`${authInput} pl-14 pr-4`}
                {...register('firstName', { required: 'First name is required' })} />
            </div>
            <FieldError error={errors.firstName} />
          </div>
          <div>
            <label className="sr-only" htmlFor="lastName">Last name</label>
            <div className="relative">
              <FieldIcon icon={FiUser} />
              <input id="lastName" placeholder="Last name" className={`${authInput} pl-14 pr-4`}
                {...register('lastName', { required: 'Last name is required' })} />
            </div>
            <FieldError error={errors.lastName} />
          </div>
        </div>

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

        <div>
          <label className="sr-only" htmlFor="phoneNumber">Phone (optional)</label>
          <div className="relative">
            <FieldIcon icon={FiPhone} />
            <input id="phoneNumber" placeholder="Phone (optional)" className={`${authInput} pl-14 pr-4`}
              {...register('phoneNumber')} />
          </div>
        </div>

        <div>
          <label className="sr-only" htmlFor="password">Password</label>
          <div className="relative">
            <FieldIcon icon={FiLock} />
            <input id="password" type={showPassword ? 'text' : 'password'} placeholder="Password"
              className={`${authInput} pl-14 pr-12`}
              {...register('password', {
                required: 'Password is required',
                minLength: { value: 8, message: 'At least 8 characters' },
                pattern: { value: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/, message: 'Use upper, lower & a digit' },
              })} />
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

        <div>
          <label className="sr-only" htmlFor="confirmPassword">Confirm password</label>
          <div className="relative">
            <FieldIcon icon={FiShield} />
            <input id="confirmPassword" type={showPassword ? 'text' : 'password'} placeholder="Confirm password"
              className={`${authInput} pl-14 pr-4`}
              {...register('confirmPassword', { validate: (v) => v === password || 'Passwords do not match' })} />
          </div>
          <FieldError error={errors.confirmPassword} />
        </div>

        <button
          type="submit"
          className="btn-primary w-full !rounded-full py-3.5 text-sm font-semibold uppercase tracking-wider"
          disabled={isSubmitting}
        >
          {isSubmitting ? <Spinner className="h-4 w-4 text-white" /> : 'Create account'}
        </button>
      </form>

      <p className="mt-8 text-center text-sm text-slate-500 dark:text-slate-400">
        Already have an account?{' '}
        <Link to="/login" className="font-semibold text-brand-600 hover:text-brand-700 dark:text-brand-400">Sign in</Link>
      </p>
    </AuthCard>
  );
}
