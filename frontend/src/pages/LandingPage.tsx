import {
  ArrowRight,
  CheckCircle2,
  FileSearch,
  Lock,
  MessageSquareText,
  ScanSearch,
  ShieldCheck,
  Sparkles,
  Upload,
  Users,
} from 'lucide-react'
import { Button } from '../components/ui/Button'
import { SectionLabel } from '../components/ui/SectionLabel'

const FEATURES = [
  {
    icon: ScanSearch,
    title: 'Semantic search',
    description:
      'Find relevant passages across your PDFs by meaning, not just keywords, using vector similarity search.',
  },
  {
    icon: Sparkles,
    title: 'Grounded AI answers',
    description:
      'Every answer is generated from retrieved document passages, never from unsupported general knowledge.',
  },
  {
    icon: FileSearch,
    title: 'Verifiable citations',
    description:
      'Click any citation to jump straight to the source document and page that supports the answer.',
  },
  {
    icon: MessageSquareText,
    title: 'Document summaries',
    description:
      'Generate short, detailed, or key-point summaries of long documents on demand.',
  },
  {
    icon: Lock,
    title: 'Private by default',
    description:
      'Your documents are only ever visible to you. Ownership is enforced on every request, every time.',
  },
  {
    icon: Users,
    title: 'Built for teams and research',
    description:
      'Organize handbooks, papers, and policies in one searchable library your whole workflow can rely on.',
  },
]

const STEPS = [
  {
    number: '01',
    icon: Upload,
    title: 'Upload',
    description: 'Drag in one or more PDF documents. DocuMind extracts and indexes the text, page by page.',
  },
  {
    number: '02',
    icon: MessageSquareText,
    title: 'Ask',
    description: 'Ask a question in plain language, scoped to one document, several, or your whole library.',
  },
  {
    number: '03',
    icon: FileSearch,
    title: 'Discover',
    description: 'Get a grounded answer with citations you can click to verify against the original source.',
  },
]

const TRUST_POINTS = [
  'Answers are generated only from passages actually retrieved for you',
  'Every citation links back to a real page in a document you uploaded',
  'If nothing relevant is found, DocuMind says so instead of guessing',
  'Ownership is enforced on every request — your documents are only ever yours',
]

const STACK = ['React', 'TypeScript', '.NET', 'PostgreSQL', 'pgvector']

export function LandingPage() {
  return (
    <main>
      <section className="relative overflow-hidden bg-slate-950">
        <div className="bg-dot-grid-dark pointer-events-none absolute inset-0" aria-hidden="true" />
        <div
          className="pointer-events-none absolute -left-24 -top-24 h-[420px] w-[420px] rounded-full bg-brand-500/30 blur-3xl"
          aria-hidden="true"
        />
        <div
          className="pointer-events-none absolute right-0 top-1/3 h-[380px] w-[380px] translate-x-1/3 rounded-full bg-indigo-500/20 blur-3xl"
          aria-hidden="true"
        />

        <div className="relative mx-auto grid max-w-6xl gap-16 px-4 py-20 sm:px-6 sm:py-28 lg:grid-cols-[1.05fr_1fr] lg:items-center lg:px-8">
          <div className="animate-fade-in-up">
            <h1 className="text-4xl font-semibold leading-[1.1] tracking-tight text-white sm:text-5xl lg:text-[3.4rem]">
              Ask your documents anything.{' '}
              <span className="bg-gradient-to-r from-brand-400 to-sky-300 bg-clip-text text-transparent">
                Get answers you can verify.
              </span>
            </h1>
            <p className="mt-6 max-w-xl text-lg text-slate-300">
              DocuMind AI turns your PDFs into a searchable knowledge base. Upload handbooks, papers,
              or policies, ask natural-language questions, and get grounded answers with real source
              citations &mdash; never invented page numbers or quotes.
            </p>
            <div className="mt-10 flex flex-col items-start gap-3 sm:flex-row sm:items-center">
              <Button to="/register" size="lg" rightIcon={<ArrowRight className="h-4 w-4" aria-hidden="true" />}>
                Start Searching
              </Button>
              <Button
                to="#features"
                variant="secondary"
                size="lg"
                className="!border-white/20 !bg-white/5 !text-white hover:!bg-white/10"
              >
                Explore Features
              </Button>
            </div>

            <div className="mt-12 flex flex-wrap items-center gap-x-5 gap-y-2">
              <span className="text-xs font-medium uppercase tracking-wider text-slate-500">Built with</span>
              {STACK.map((tech) => (
                <span key={tech} className="text-sm font-medium text-slate-400">
                  {tech}
                </span>
              ))}
            </div>
          </div>

          <div className="animate-fade-in-up relative" style={{ animationDelay: '150ms' }}>
            <div className="overflow-hidden rounded-xl border border-white/10 bg-slate-900 shadow-2xl shadow-black/40">
              <div className="flex items-center gap-2 border-b border-white/10 bg-slate-800/60 px-4 py-2.5">
                <span className="h-2.5 w-2.5 rounded-full bg-red-400/70" aria-hidden="true" />
                <span className="h-2.5 w-2.5 rounded-full bg-amber-400/70" aria-hidden="true" />
                <span className="h-2.5 w-2.5 rounded-full bg-emerald-400/70" aria-hidden="true" />
                <span className="ml-3 truncate rounded-md bg-slate-900/80 px-3 py-1 text-xs text-slate-400">
                  documind.ai/chat
                </span>
              </div>

              <div className="bg-slate-900 p-5">
                <p className="mb-3 text-center text-[11px] font-medium uppercase tracking-wide text-slate-400">
                  Illustrative example &mdash; not a live query
                </p>

                <div className="flex justify-end">
                  <div className="max-w-[80%] rounded-xl bg-brand-600 px-4 py-2.5 text-sm text-white shadow-sm">
                    When is the final exam registration deadline?
                  </div>
                </div>

                <div className="mt-3 flex justify-start">
                  <div className="max-w-[90%] rounded-xl border border-white/10 bg-slate-800/80 p-4 text-sm">
                    <p className="text-slate-200">
                      The handbook states that exam registration closes two weeks before the exam
                      period begins <span className="font-medium text-brand-300">[1]</span>, and the
                      exam regulations confirm no late registrations are accepted after that date{' '}
                      <span className="font-medium text-brand-300">[2]</span>.
                    </p>

                    <ul className="mt-3 flex flex-col gap-2 border-t border-white/10 pt-3">
                      <li className="rounded-lg bg-slate-900/60 p-2 text-xs text-slate-400">
                        <span className="font-medium text-slate-300">[1] University Handbook.pdf</span>
                        <span className="ml-2 rounded-full bg-white/10 px-2 py-0.5 text-slate-400">p. 12</span>
                      </li>
                      <li className="rounded-lg bg-slate-900/60 p-2 text-xs text-slate-400">
                        <span className="font-medium text-slate-300">[2] Exam Regulations.pdf</span>
                        <span className="ml-2 rounded-full bg-white/10 px-2 py-0.5 text-slate-400">p. 24</span>
                      </li>
                    </ul>
                  </div>
                </div>
              </div>
            </div>

            <div
              className="pointer-events-none absolute -bottom-6 -right-6 -z-10 h-40 w-40 rounded-full bg-brand-500/30 blur-3xl"
              aria-hidden="true"
            />
          </div>
        </div>
      </section>

      <section id="how-it-works" className="bg-slate-950 px-4 py-20 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-6xl">
          <div className="mx-auto max-w-2xl text-center">
            <SectionLabel>How it works</SectionLabel>
            <h2 className="mt-3 text-3xl text-white">Three steps from PDF to answer</h2>
            <p className="mt-3 text-slate-400">
              No manual tagging, no reading hundreds of pages. DocuMind handles extraction, indexing,
              and retrieval for you.
            </p>
          </div>

          <div className="mt-14 grid gap-8 sm:grid-cols-3">
            {STEPS.map((step, i) => (
              <div
                key={step.number}
                className="animate-fade-in-up group relative overflow-hidden rounded-2xl border border-slate-800 bg-slate-900/60 p-6 transition-all duration-200 hover:-translate-y-1 hover:border-brand-500/40 hover:shadow-lg hover:shadow-black/30"
                style={{ animationDelay: `${i * 100}ms` }}
              >
                <div
                  className="pointer-events-none absolute -right-8 -top-8 h-24 w-24 rounded-full bg-brand-500/20 opacity-0 blur-2xl transition-opacity duration-300 group-hover:opacity-70"
                  aria-hidden="true"
                />
                <div className="relative flex items-center justify-between">
                  <span className="flex h-11 w-11 items-center justify-center rounded-xl bg-brand-500/15 text-brand-400 transition-colors group-hover:bg-brand-600 group-hover:text-white">
                    <step.icon className="h-5 w-5" aria-hidden="true" />
                  </span>
                  <span className="text-sm font-semibold text-slate-400">{step.number}</span>
                </div>
                <h3 className="relative mt-4 text-xl text-white">{step.title}</h3>
                <p className="relative mt-2 text-sm text-slate-400">{step.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section id="features" className="border-t border-slate-800/60 bg-slate-900 py-20">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-2xl text-center">
            <SectionLabel>Features</SectionLabel>
            <h2 className="mt-3 text-3xl text-white">Everything you need to search with confidence</h2>
          </div>

          <div className="mt-14 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {FEATURES.map((feature, i) => (
              <div
                key={feature.title}
                className="animate-fade-in-up group relative overflow-hidden rounded-2xl bg-slate-800 p-6 shadow-sm ring-1 ring-slate-700 transition-all duration-200 hover:-translate-y-1 hover:shadow-lg hover:shadow-black/30 hover:ring-brand-500/40"
                style={{ animationDelay: `${i * 75}ms` }}
              >
                <div
                  className="pointer-events-none absolute -right-8 -top-8 h-24 w-24 rounded-full bg-brand-500/20 opacity-0 blur-2xl transition-opacity duration-300 group-hover:opacity-70"
                  aria-hidden="true"
                />
                <span className="relative flex h-10 w-10 items-center justify-center rounded-lg bg-brand-500/15 text-brand-400 transition-colors group-hover:bg-brand-600 group-hover:text-white">
                  <feature.icon className="h-5 w-5" aria-hidden="true" />
                </span>
                <h3 className="relative mt-4 text-lg text-white">{feature.title}</h3>
                <p className="relative mt-2 text-sm text-slate-400">{feature.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section id="privacy" className="border-t border-slate-800/60 bg-slate-950 px-4 py-20 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-6xl">
          <div className="grid gap-12 lg:grid-cols-2 lg:items-center">
            <div>
              <SectionLabel>Privacy &amp; trust</SectionLabel>
              <h2 className="mt-3 text-3xl text-white">Grounded answers. Verifiable sources. Private by default.</h2>
              <p className="mt-4 text-slate-400">
                DocuMind only answers from the documents you upload, and only you can access your own
                documents and conversations.
              </p>
              <ul className="mt-6 flex flex-col gap-3">
                {TRUST_POINTS.map((point) => (
                  <li key={point} className="flex items-start gap-3 text-sm text-slate-300">
                    <CheckCircle2 className="mt-0.5 h-4.5 w-4.5 shrink-0 text-brand-400" aria-hidden="true" />
                    {point}
                  </li>
                ))}
              </ul>
            </div>

            <div className="relative overflow-hidden rounded-2xl border border-slate-700 bg-gradient-to-br from-slate-800 to-slate-900 p-10 text-center shadow-sm">
              <span className="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-brand-600 text-white shadow-lg shadow-brand-600/30">
                <ShieldCheck className="h-7 w-7" aria-hidden="true" />
              </span>
              <p className="mt-5 text-sm text-slate-400">
                If the available documents don't contain a reliable answer, DocuMind tells you so
                instead of guessing &mdash; the same standard applied to every search, every citation,
                every summary.
              </p>
            </div>
          </div>
        </div>
      </section>

      <section className="border-t border-slate-800 bg-slate-950">
        <div className="mx-auto max-w-4xl px-4 py-16 text-center sm:px-6 lg:px-8">
          <h2 className="text-2xl text-white sm:text-3xl">Ready to see it work on your own documents?</h2>
          <p className="mx-auto mt-3 max-w-xl text-slate-400">
            Create a free account and upload your first PDF &mdash; no credit card, no paid API key
            required to try it.
          </p>
          <div className="mt-8">
            <Button to="/register" size="lg" rightIcon={<ArrowRight className="h-4 w-4" aria-hidden="true" />}>
              Start Searching
            </Button>
          </div>
        </div>
      </section>
    </main>
  )
}
