import { useCallback, useEffect, useState } from 'react';
import { FiShield, FiEdit2 } from 'react-icons/fi';
import { adminApi } from '../../api';
import { getErrorMessage } from '../../api/client';
import { useToast } from '../../contexts/ToastContext';
import { PageHeader, LoadingScreen, Spinner } from '../../components/ui';
import Modal from '../../components/Modal';

export default function AdminRoles() {
  const { toast } = useToast();
  const [roles, setRoles] = useState(null);
  const [permissions, setPermissions] = useState([]);
  const [editRole, setEditRole] = useState(null);

  const load = useCallback(() => {
    Promise.all([adminApi.roles(), adminApi.permissions()])
      .then(([r, p]) => { setRoles(r); setPermissions(p); })
      .catch((e) => { toast(getErrorMessage(e), 'error'); setRoles([]); });
  }, [toast]);

  useEffect(() => { load(); }, [load]);

  if (roles === null) return <LoadingScreen />;

  // Group permissions by category for the editor.
  const byCategory = permissions.reduce((acc, p) => { (acc[p.category] ||= []).push(p); return acc; }, {});

  return (
    <div>
      <PageHeader icon={FiShield} title="Roles & Permissions" subtitle="Role-based access control" />

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
        {roles.map((role) => (
          <div key={role.id} className="card card-hover p-5">
            <div className="flex items-start justify-between">
              <div className="flex items-center gap-2">
                <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-accent-500 text-white"><FiShield /></span>
                <div>
                  <h3 className="font-semibold text-slate-900 dark:text-white">{role.name}</h3>
                  <p className="text-xs text-slate-500">{role.userCount} user{role.userCount === 1 ? '' : 's'}</p>
                </div>
              </div>
              <button className="btn-secondary" onClick={() => setEditRole(role)}><FiEdit2 /> Permissions</button>
            </div>
            <div className="mt-3 flex flex-wrap gap-1">
              {role.permissions.length ? role.permissions.map((p) => (
                <span key={p.id} className="badge bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300">{p.name}</span>
              )) : <span className="text-sm text-slate-400">No permissions assigned</span>}
            </div>
          </div>
        ))}
      </div>

      <EditPermissionsModal role={editRole} byCategory={byCategory} onClose={() => setEditRole(null)} onSaved={() => { setEditRole(null); load(); }} />
    </div>
  );
}

function EditPermissionsModal({ role, byCategory, onClose, onSaved }) {
  const { toast } = useToast();
  const [selected, setSelected] = useState(new Set());
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (role) setSelected(new Set(role.permissions.map((p) => p.id)));
  }, [role]);

  const toggle = (id) => setSelected((prev) => {
    const next = new Set(prev);
    if (next.has(id)) next.delete(id); else next.add(id);
    return next;
  });

  const save = async () => {
    setSaving(true);
    try {
      await adminApi.updateRolePermissions(role.id, [...selected]);
      toast('Permissions updated.', 'success');
      onSaved();
    } catch (e) { toast(getErrorMessage(e), 'error'); }
    finally { setSaving(false); }
  };

  return (
    <Modal open={!!role} onClose={onClose} title={role ? `${role.name} permissions` : ''}
      footer={<><button className="btn-secondary" onClick={onClose}>Cancel</button>
        <button className="btn-primary" onClick={save} disabled={saving}>{saving && <Spinner className="h-4 w-4 text-white" />} Save</button></>}>
      <div className="space-y-4">
        {Object.entries(byCategory).map(([category, perms]) => (
          <div key={category}>
            <h4 className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-400">{category}</h4>
            <div className="space-y-1">
              {perms.map((p) => (
                <label key={p.id} className="flex items-center gap-2 rounded-lg p-2 text-sm text-slate-700 hover:bg-slate-50 dark:text-slate-200 dark:hover:bg-slate-800">
                  <input type="checkbox" checked={selected.has(p.id)} onChange={() => toggle(p.id)} />
                  <span className="font-medium">{p.name}</span>
                  {p.description && p.description !== p.name && <span className="text-xs text-slate-400">— {p.description}</span>}
                </label>
              ))}
            </div>
          </div>
        ))}
      </div>
    </Modal>
  );
}
