import { useCallback, useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { FiPlus, FiHome, FiTrash2, FiX } from 'react-icons/fi';
import { adminApi } from '../../api';
import { getErrorMessage } from '../../api/client';
import { useToast } from '../../contexts/ToastContext';
import { PageHeader, EmptyState, Skeleton, Spinner } from '../../components/ui';
import Modal from '../../components/Modal';

export default function AdminOrganizations() {
  const { toast } = useToast();
  const [orgs, setOrgs] = useState(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [deptFor, setDeptFor] = useState(null);
  const [newDept, setNewDept] = useState('');

  const load = useCallback(() => {
    adminApi.organizations().then(setOrgs).catch((e) => { toast(getErrorMessage(e), 'error'); setOrgs([]); });
  }, [toast]);

  useEffect(() => { load(); }, [load]);

  const deleteOrg = async (org) => {
    try { await adminApi.deleteOrganization(org.id); toast('Organization deleted.', 'success'); load(); }
    catch (e) { toast(getErrorMessage(e), 'error'); }
  };
  const addDept = async () => {
    if (!newDept.trim()) return;
    try { await adminApi.addDepartment(deptFor.id, { name: newDept.trim(), description: null }); setNewDept(''); toast('Department added.', 'success'); load(); }
    catch (e) { toast(getErrorMessage(e), 'error'); }
  };
  const removeDept = async (orgId, deptId) => {
    try { await adminApi.removeDepartment(orgId, deptId); toast('Department removed.', 'success'); load(); }
    catch (e) { toast(getErrorMessage(e), 'error'); }
  };

  // Keep the open department modal in sync with reloaded data.
  const deptForLive = deptFor && orgs ? orgs.find((o) => o.id === deptFor.id) : deptFor;

  return (
    <div>
      <PageHeader icon={FiHome} title="Organizations" subtitle="Client organizations and their departments"
        actions={<button className="btn-primary" onClick={() => setCreateOpen(true)}><FiPlus /> New organization</button>} />

      {orgs === null ? (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">{Array.from({ length: 2 }).map((_, i) => <Skeleton key={i} className="h-40 w-full" />)}</div>
      ) : orgs.length ? (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          {orgs.map((o) => (
            <div key={o.id} className="card card-hover p-5">
              <div className="flex items-start justify-between">
                <div className="flex items-center gap-3">
                  <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-accent-500 text-white"><FiHome /></span>
                  <div>
                    <h3 className="font-semibold text-slate-900 dark:text-white">{o.name}</h3>
                    <p className="text-sm text-slate-500">{o.industry || '—'}{o.location ? ` · ${o.location}` : ''}</p>
                  </div>
                </div>
                <span className={o.isActive ? 'badge-emerald' : 'badge bg-slate-200 text-slate-600'}>
                  {o.isActive ? 'Active' : 'Inactive'}
                </span>
              </div>
              {o.description && <p className="mt-2 text-sm text-slate-500">{o.description}</p>}
              <div className="mt-3 flex gap-4 text-xs text-slate-400">
                <span>{o.departmentCount} departments</span>
                <span>{o.jobCount} jobs</span>
              </div>
              <div className="mt-3 flex flex-wrap gap-1">
                {o.departments.map((d) => (
                  <span key={d.id} className="badge bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300">{d.name} ({d.jobCount})</span>
                ))}
              </div>
              <div className="mt-4 flex items-center gap-2 border-t border-slate-100 pt-3 dark:border-slate-800">
                <button className="btn-secondary flex-1" onClick={() => setDeptFor(o)}>Manage departments</button>
                <button className="btn-secondary text-red-600" onClick={() => deleteOrg(o)} aria-label="Delete"><FiTrash2 /></button>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <EmptyState title="No organizations" icon={FiHome}
          action={<button className="btn-primary mt-2" onClick={() => setCreateOpen(true)}>New organization</button>} />
      )}

      <CreateOrgModal open={createOpen} onClose={() => setCreateOpen(false)} onSaved={() => { setCreateOpen(false); load(); }} />

      <Modal open={!!deptFor} onClose={() => { setDeptFor(null); setNewDept(''); }} title={`Departments — ${deptForLive?.name || ''}`}
        footer={<button className="btn-secondary" onClick={() => { setDeptFor(null); setNewDept(''); }}>Close</button>}>
        <div className="space-y-3">
          {deptForLive?.departments?.length ? deptForLive.departments.map((d) => (
            <div key={d.id} className="flex items-center justify-between rounded-lg border border-slate-200 p-3 dark:border-slate-700">
              <div>
                <div className="text-sm font-medium text-slate-800 dark:text-slate-100">{d.name}</div>
                <div className="text-xs text-slate-400">{d.jobCount} jobs</div>
              </div>
              <button onClick={() => removeDept(deptForLive.id, d.id)} className="text-slate-400 hover:text-red-500"><FiX /></button>
            </div>
          )) : <p className="text-sm text-slate-400">No departments yet.</p>}
          <div className="flex gap-2">
            <input className="input" placeholder="New department name" value={newDept} onChange={(e) => setNewDept(e.target.value)} />
            <button className="btn-primary" onClick={addDept}><FiPlus /> Add</button>
          </div>
        </div>
      </Modal>
    </div>
  );
}

function CreateOrgModal({ open, onClose, onSaved }) {
  const { toast } = useToast();
  const { register, handleSubmit, reset, formState: { isSubmitting } } = useForm({
    defaultValues: { name: '', description: '', industry: '', website: '', location: '', isActive: true },
  });
  useEffect(() => { if (open) reset(); }, [open, reset]);

  const submit = async (values) => {
    try { await adminApi.createOrganization(values); toast('Organization created.', 'success'); onSaved(); }
    catch (e) { toast(getErrorMessage(e), 'error'); }
  };

  return (
    <Modal open={open} onClose={onClose} title="Create organization"
      footer={<><button className="btn-secondary" onClick={onClose}>Cancel</button>
        <button className="btn-primary" onClick={handleSubmit(submit)} disabled={isSubmitting}>{isSubmitting && <Spinner className="h-4 w-4 text-white" />} Create</button></>}>
      <div className="space-y-3">
        <div><label className="label">Name</label><input className="input" {...register('name', { required: true })} /></div>
        <div><label className="label">Description</label><textarea className="input min-h-[70px]" {...register('description')} /></div>
        <div className="grid grid-cols-2 gap-3">
          <div><label className="label">Industry</label><input className="input" {...register('industry')} /></div>
          <div><label className="label">Location</label><input className="input" {...register('location')} /></div>
        </div>
        <div><label className="label">Website</label><input className="input" {...register('website')} /></div>
        <label className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-300">
          <input type="checkbox" {...register('isActive')} /> Active
        </label>
      </div>
    </Modal>
  );
}
