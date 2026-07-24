import { useCallback, useEffect, useRef, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { FiSend, FiMessageSquare, FiArrowLeft } from 'react-icons/fi';
import { messagesApi } from '../api';
import { getErrorMessage } from '../api/client';
import { useToast } from '../contexts/ToastContext';
import { PageHeader, Spinner, EmptyState } from '../components/ui';

const POLL_MS = 15000;

export default function Messages() {
  const { userId } = useParams();
  const navigate = useNavigate();
  const { toast } = useToast();
  const [conversations, setConversations] = useState([]);
  const [thread, setThread] = useState([]);
  const [body, setBody] = useState('');
  const [sending, setSending] = useState(false);
  const bottomRef = useRef(null);

  const loadConversations = useCallback(() => {
    messagesApi.conversations().then(setConversations).catch(() => {});
  }, []);

  const loadThread = useCallback(() => {
    if (!userId) { setThread([]); return; }
    messagesApi.thread(userId, { pageSize: 200 }).then((d) => setThread(d.items)).catch((e) => toast(getErrorMessage(e), 'error'));
  }, [userId, toast]);

  useEffect(() => { loadConversations(); }, [loadConversations]);
  useEffect(() => {
    loadThread();
    if (!userId) return;
    const t = setInterval(() => { loadThread(); loadConversations(); }, POLL_MS);
    return () => clearInterval(t);
  }, [loadThread, loadConversations, userId]);

  useEffect(() => { bottomRef.current?.scrollIntoView({ behavior: 'smooth' }); }, [thread]);

  const send = async () => {
    if (!body.trim() || !userId) return;
    setSending(true);
    try {
      await messagesApi.send({ recipientUserId: userId, subject: null, body: body.trim() });
      setBody('');
      loadThread();
      loadConversations();
    } catch (e) {
      toast(getErrorMessage(e), 'error');
    } finally {
      setSending(false);
    }
  };

  const activeConv = conversations.find((c) => c.otherUserId === userId);
  const headerName = activeConv?.otherUserName || thread.find((m) => !m.isMine)?.senderName || 'Conversation';

  return (
    <div>
      <PageHeader icon={FiMessageSquare} title="Messages" subtitle="Your conversations" />
      <div className="card grid grid-cols-1 overflow-hidden md:grid-cols-3" style={{ height: '32rem' }}>
        {/* Conversation list */}
        <div className={`border-r border-slate-100 dark:border-slate-800 md:col-span-1 ${userId ? 'hidden md:block' : ''} overflow-y-auto`}>
          {conversations.length ? conversations.map((c) => (
            <button
              key={c.otherUserId}
              onClick={() => navigate(`/messages/${c.otherUserId}`)}
              className={`flex w-full items-center justify-between gap-2 border-b border-slate-50 px-4 py-3 text-left hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800 ${c.otherUserId === userId ? 'bg-brand-50/50 dark:bg-brand-900/10' : ''}`}
            >
              <div className="min-w-0">
                <div className="truncate text-sm font-medium text-slate-800 dark:text-slate-100">{c.otherUserName}</div>
                <div className="truncate text-xs text-slate-500">{c.lastMessage}</div>
              </div>
              {c.unreadCount > 0 && (
                <span className="flex h-5 min-w-5 items-center justify-center rounded-full bg-gradient-to-br from-brand-600 to-accent-500 px-1 text-xs font-semibold text-white">{c.unreadCount}</span>
              )}
            </button>
          )) : (
            <p className="px-4 py-8 text-center text-sm text-slate-400">No conversations yet</p>
          )}
        </div>

        {/* Thread */}
        <div className="flex flex-col md:col-span-2">
          {userId ? (
            <>
              <div className="flex items-center gap-2 border-b border-slate-100 px-4 py-3 dark:border-slate-800">
                <button className="md:hidden" onClick={() => navigate('/messages')}><FiArrowLeft /></button>
                <span className="font-semibold text-slate-800 dark:text-slate-100">{headerName}</span>
              </div>
              <div className="flex-1 space-y-3 overflow-y-auto p-4">
                {thread.length ? thread.map((m) => (
                  <div key={m.id} className={`flex ${m.isMine ? 'justify-end' : 'justify-start'}`}>
                    <div className={`max-w-[75%] rounded-2xl px-4 py-2 text-sm shadow-sm ${m.isMine ? 'rounded-br-sm bg-gradient-to-br from-brand-600 to-brand-500 text-white' : 'rounded-bl-sm bg-slate-100 text-slate-800 dark:bg-slate-800 dark:text-slate-100'}`}>
                      {m.subject && <div className="mb-0.5 text-xs font-semibold opacity-80">{m.subject}</div>}
                      <div className="whitespace-pre-line">{m.body}</div>
                      <div className={`mt-1 text-[10px] ${m.isMine ? 'text-brand-100' : 'text-slate-400'}`}>{new Date(m.sentAt).toLocaleString()}</div>
                    </div>
                  </div>
                )) : (
                  <p className="py-16 text-center text-sm text-slate-400">No messages yet — say hello.</p>
                )}
                <div ref={bottomRef} />
              </div>
              <div className="flex items-center gap-2 border-t border-slate-100 p-3 dark:border-slate-800">
                <input
                  className="input"
                  placeholder="Type a message…"
                  value={body}
                  onChange={(e) => setBody(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && !e.shiftKey && (e.preventDefault(), send())}
                />
                <button className="btn-primary" onClick={send} disabled={sending || !body.trim()}>
                  {sending ? <Spinner className="h-4 w-4 text-white" /> : <FiSend />}
                </button>
              </div>
            </>
          ) : (
            <div className="flex flex-1 items-center justify-center">
              <EmptyState title="Select a conversation" message="Choose a conversation to view messages." icon={FiMessageSquare} />
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
