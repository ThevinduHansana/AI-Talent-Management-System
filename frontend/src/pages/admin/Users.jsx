import { useCallback, useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { FiPlus, FiSearch, FiEdit2, FiUsers } from 'react-icons/fi';
import { adminApi } from '../../api';
import { getErrorMessage } from '../../api/client';
import { useToast } from '../../contexts/ToastContext';
import { ROLES } from '../../constants';
import { PageHeader, Pagination, EmptyState, Skeleton, Spinner } from '../../components/ui';
import Modal from '../../components/Modal';

const ALL_ROLES = [ROLES.CANDIDATE, ROLES.RECRUITER, ROLES.HIRING_MANAGER, ROLES.ADMINISTRATOR];

export default function AdminUsers() {
  const { toast } = useToast();
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [orgs, setOrgs] = useState([]);
  const [filters, setFilters] = useState({ search: '', role: '', isActive: '', page: 1 });
  const [createOpen, setCreateOpen] = useState(false);
  const [editUser, setEditUser] = useState(null);

  const load = useCallback(() => {
    setLoading(true);
    const params = { page: filters.page, pageSize: 10 };
    if (filters.search) params.search = filters.search;
    if (filters.role) params.role = filters.role;
    if (filters.isActive !== '') params.isActive = filters.isActive;
    adminApi.users(params).then(setData).catch((e) => toast(getErrorMessage(e), 'error')).finally(() => setLoading(false));
  }, [filters, toast]);

  useEffect(() => { load(); }, [load]);
  useEffect(() => { adminApi.organizations().then(setOrgs).catch(() => {}); }, []);

  const update = (patch) => setFilters((f) => ({ ...f, ...patch, page: patch.page ?? 1 }));

  return (
    <div>
      <PageHeader icon={FiUsers} title="Users" subtitle="Manage accounts, roles and access"
        actions={<button className="btn-primary" onClick={() => setCreateOpen(true)}><FiPlus /> New user</button>} />

      <div className="card mb-4 grid grid-cols-1 gap-3 p-4 sm:grid-cols-4">
        <div className="relative sm:col-span-2">
          <FiSearch className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
          <input className="input pl-9" placeholder="Search name or email…" value={filters.search} onChange={(e) => update({ search: e.target.value })} />
        </div>
        <select className="input" value={filters.role} onChange={(e) => update({ role: e.target.value })}>
          <option value="">All roles</option>
          {ALL_ROLES.map((r) => <option key={r} value={r}>{r}</option>)}
        </select>
        <select className="input" value={filters.isActive} onChange={(e) => update({ isActive: e.target.value })}>
          <option value="">Any status</option>
          <option value="true">Active</option>
          <option value="false">Inactive</option>
        </select>
      </div>

      {loading ? (
        <div className="space-y-3">{Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} className="h-16 w-full" />)}</div>
      ) : data?.items?.length ? (
        <>
          <div className="table-wrap">
            <table className="table-base">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Roles</th>
                  <th>Organization</th>
                  <th>Status</th>
                  <th className="text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((u) => (
                  <tr key={u.id}>
                    <td>
                      <div className="font-medium text-slate-800 dark:text-slate-100">{u.firstName} {u.lastName}</div>
                      <div className="text-xs text-slate-500">{u.email}</div>
                    </td>
                    <td>
                      <div className="flex flex-wrap gap-1">
                        {u.roles.map((r) => <span key={r} className="badge-brand">{r}</span>)}
                      </div>
                    </td>
                    <td className="text-slate-500">{u.organizationName || '—'}</td>
                    <td>
                      <span className={u.isActive ? 'badge-emerald' : 'badge bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300'}>
                        {u.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className="text-right">
                      <button className="btn-secondary btn-sm" onClick={() => setEditUser(u)}><FiEdit2 /> Edit</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} onChange={(p) => update({ page: p })} />
        </>
      ) : (
        <EmptyState title="No users found" icon={FiUsers} />
      )}

      <CreateUserModal open={createOpen} orgs={orgs} onClose={() => setCreateOpen(false)} onSaved={() => { setCreateOpen(false); load(); }} />
      <EditUserModal user={editUser} orgs={orgs} onClose={() => setEditUser(null)} onSaved={() => { setEditUser(null); load(); }} />
    </div>
  );
}

function CreateUserModal({ open, orgs, onClose, onSaved }) {
  const { toast } = useToast();
  const { register, handleSubmit, watch, reset, formState: { isSubmitting } } = useForm({
    defaultValues: { firstName: '', lastName: '', email: '', password: '', phoneNumber: '', role: ROLES.RECRUITER, organizationId: '', departmentId: '' },
  });
  const orgId = watch('organizationId');
  const depts = orgs.find((o) => o.id === orgId)?.departments || [];

  useEffect(() => { if (open) reset(); }, [open, reset]);

  const submit = async (values) => {
    try {
      await adminApi.createUser({
        ...values,
        phoneNumber: values.phoneNumber || null,
        organizationId: values.organizationId || null,
        departmentId: values.departmentId || null,
      });
      toast('User created.', 'success');
      onSaved();
    } catch (e) { toast(getErrorMessage(e), 'error'); }
  };

  return (
    <Modal open={open} onClose={onClose} title="Create user"
      footer={<><button className="btn-secondary" onClick={onClose}>Cancel</button>
        <button className="btn-primary" onClick={handleSubmit(submit)} disabled={isSubmitting}>{isSubmitting && <Spinner className="h-4 w-4 text-white" />} Create</button></>}>
      <div className="space-y-3">
        <div className="grid grid-cols-2 gap-3">
          <div><label className="label">First name</label><input className="input" {...register('firstName', { required: true })} /></div>
          <div><label className="label">Last name</label><input className="input" {...register('lastName', { required: true })} /></div>
        </div>
        <div><label className="label">Email</label><input type="email" className="input" {...register('email', { required: true })} /></div>
        <div><label className="label">Temporary password</label><input className="input" {...register('password', { required: true })} placeholder="Min 8 chars, upper/lower/digit" /></div>
        <div><label className="label">Phone (optional)</label><input className="input" {...register('phoneNumber')} /></div>
        <div>
          <label className="label">Role</label>
          <select className="input" {...register('role')}>{ALL_ROLES.map((r) => <option key={r} value={r}>{r}</option>)}</select>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="label">Organization</label>
            <select className="input" {...register('organizationId')}>
              <option value="">None</option>
              {orgs.map((o) => <option key={o.id} value={o.id}>{o.name}</option>)}
            </select>
          </div>
          <div>
            <label className="label">Department</label>
            <select className="input" {...register('departmentId')} disabled={!orgId}>
              <option value="">None</option>
              {depts.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
            </select>
          </div>
        </div>
      </div>
    </Modal>
  );
}

function EditUserModal({ user, orgs, onClose, onSaved }) {
  const { toast } = useToast();
  const { register, handleSubmit, watch, reset, setValue, formState: { isSubmitting } } = useForm();
  const orgId = watch('organizationId');
  const roles = watch('roles') || [];
  const depts = orgs.find((o) => o.id === orgId)?.departments || [];

  useEffect(() => {
    if (user) reset({
      firstName: user.firstName, lastName: user.lastName, phoneNumber: user.phoneNumber || '',
      isActive: user.isActive, roles: user.roles, organizationId: user.organizationId || '', departmentId: '',
    });
  }, [user, reset]);

  const toggleRole = (role) => {
    const next = roles.includes(role) ? roles.filter((r) => r !== role) : [...roles, role];
    setValue('roles', next, { shouldDirty: true });
  };

  const submit = async (values) => {
    try {
      await adminApi.updateUser(user.id, {
        firstName: values.firstName, lastName: values.lastName, phoneNumber: values.phoneNumber || null,
        isActive: values.isActive, roles: values.roles,
        organizationId: values.organizationId || null, departmentId: values.departmentId || null,
      });
      toast('User updated.', 'success');
      onSaved();
    } catch (e) { toast(getErrorMessage(e), 'error'); }
  };

  return (
    <Modal open={!!user} onClose={onClose} title={user ? `Edit ${user.firstName} ${user.lastName}` : ''}
      footer={<><button className="btn-secondary" onClick={onClose}>Cancel</button>
        <button className="btn-primary" onClick={handleSubmit(submit)} disabled={isSubmitting}>{isSubmitting && <Spinner className="h-4 w-4 text-white" />} Save</button></>}>
      <div className="space-y-3">
        <div className="grid grid-cols-2 gap-3">
          <div><label className="label">First name</label><input className="input" {...register('firstName', { required: true })} /></div>
          <div><label className="label">Last name</label><input className="input" {...register('lastName', { required: true })} /></div>
        </div>
        <div><label className="label">Phone</label><input className="input" {...register('phoneNumber')} /></div>
        <div>
          <label className="label">Roles</label>
          <div className="flex flex-wrap gap-2">
            {ALL_ROLES.map((r) => (
              <button type="button" key={r} onClick={() => toggleRole(r)}
                className={`badge cursor-pointer ${roles.includes(r) ? 'bg-brand-600 text-white' : 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300'}`}>
                {r}
              </button>
            ))}
          </div>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="label">Organization</label>
            <select className="input" {...register('organizationId')}>
              <option value="">None</option>
              {orgs.map((o) => <option key={o.id} value={o.id}>{o.name}</option>)}
            </select>
          </div>
          <div>
            <label className="label">Department</label>
            <select className="input" {...register('departmentId')} disabled={!orgId}>
              <option value="">None</option>
              {depts.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
            </select>
          </div>
        </div>
        <label className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-300">
          <input type="checkbox" {...register('isActive')} /> Account active
        </label>
      </div>
    </Modal>
  );
}
