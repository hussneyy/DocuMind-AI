import {
  ArrowRight,
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

export function LandingPage() {
  return (
    <main>
      <section className="relative overflow-hidden bg-gradient-to-b from-brand-50 to-white">
        <div className="bg-dot-grid pointer-events-none absolute inset-0" aria-hidden="true" />
        <div
          className="pointer-events-none absolute left-1/2 top-0 h-[480px] w-[480px] -translate-x-1/2 rounded-full bg-brand-300/20 blur-3xl"
          aria-hidden="true"
        />
        <div className="relative mx-auto max-w-6xl px-4 py-20 sm:px-6 sm:py-28 lg:px-8">
          <div className="animate-fade-in-up mx-auto max-w-3xl text-center">
            <p className="inline-flex items-center gap-1.5 rounded-full bg-brand-100 px-3 py-1 text-xs font-semibold text-brand-700">
              <Sparkles className="h-3.5 w-3.5" aria-hidden="true" />
              Portfolio project &middot; retrieval-augmented document QA
            </p>
            <h1 className="mt-6 text-4xl sm:text-5xl">
              Ask your documents anything.{' '}
              <span className="bg-gradient-to-r from-brand-600 to-brand-400 bg-clip-text text-transparent">
                Get answers you can verify.
              </span>
            </h1>
            <p className="mt-6 text-lg text-slate-600">
              DocuMind AI turns your PDFs into a searchable knowledge base. Upload handbooks, papers,
              or policies, ask natural-language questions, and get grounded answers with real source
              citations &mdash; never invented page numbers or quotes.
            </p>
            <div className="mt-10 flex flex-col items-center justify-center gap-3 sm:flex-row">
              <Button to="/register" size="lg" rightIcon={<ArrowRight className="h-4 w-4" aria-hidden="true" />}>
                Start Searching
              </Button>
              <Button to="#features" variant="secondary" size="lg">
                Explore Features
              </Button>
            </div>
          </div>
        </div>
      </section>

      <section id="how-it-works" className="mx-auto max-w-6xl px-4 py-20 sm:px-6 lg:px-8">
        <div className="mx-auto max-w-2xl text-center">
          <h2 className="text-3xl">Three steps from PDF to answer</h2>
          <p className="mt-3 text-slate-600">
            No manual tagging, no reading hundreds of pages. DocuMind handles extraction, indexing,
            and retrieval for you.
          </p>
        </div>

        <div className="mt-14 grid gap-8 sm:grid-cols-3">
          {STEPS.map((step, i) => (
            <div
              key={step.number}
              className="animate-fade-in-up group relative rounded-2xl border border-slate-200 p-6 transition-all duration-200 hover:-translate-y-1 hover:border-brand-200 hover:shadow-lg hover:shadow-slate-200/60"
              style={{ animationDelay: `${i * 100}ms` }}
            >
              <div className="flex items-center justify-between">
                <span className="flex h-11 w-11 items-center justify-center rounded-xl bg-brand-50 text-brand-600 transition-colors group-hover:bg-brand-600 group-hover:text-white">
                  <step.icon className="h-5 w-5" aria-hidden="true" />
                </span>
                <span className="text-sm font-semibold text-slate-500">{step.number}</span>
              </div>
              <h3 className="mt-4 text-xl">{step.title}</h3>
              <p className="mt-2 text-sm text-slate-600">{step.description}</p>
            </div>
          ))}
        </div>
      </section>

      <section id="features" className="bg-slate-50 py-20">
        <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-2xl text-center">
            <h2 className="text-3xl">Everything you need to search with confidence</h2>
          </div>

          <div className="mt-14 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {FEATURES.map((feature, i) => (
              <div
                key={feature.title}
                className="animate-fade-in-up group rounded-2xl bg-white p-6 shadow-sm ring-1 ring-slate-200 transition-all duration-200 hover:-translate-y-1 hover:shadow-lg hover:shadow-slate-200/60 hover:ring-brand-200"
                style={{ animationDelay: `${i * 75}ms` }}
              >
                <span className="flex h-10 w-10 items-center justify-center rounded-lg bg-brand-50 text-brand-600 transition-colors group-hover:bg-brand-600 group-hover:text-white">
                  <feature.icon className="h-5 w-5" aria-hidden="true" />
                </span>
                <h3 className="mt-4 text-lg">{feature.title}</h3>
                <p className="mt-2 text-sm text-slate-600">{feature.description}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="mx-auto max-w-4xl px-4 py-20 sm:px-6 lg:px-8">
        <div className="text-center">
          <h2 className="text-3xl">See it in action</h2>
          <p className="mt-3 text-slate-600">
            Illustrative example &mdash; shown for demonstration only, not a live query.
          </p>
        </div>

        <div className="mt-10 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm sm:p-8">
          <div className="flex justify-end">
            <div className="max-w-[80%] rounded-xl bg-brand-600 px-4 py-3 text-sm text-white shadow-sm">
              When is the final exam registration deadline?
            </div>
          </div>

          <div className="mt-4 flex justify-start">
            <div className="max-w-[85%] rounded-xl border border-slate-200 bg-white p-4 text-sm shadow-sm">
              <p className="text-slate-800">
                The handbook states that exam registration closes two weeks before the exam period
                begins <span className="font-medium text-brand-600">[1]</span>, and the exam
                regulations confirm no late registrations are accepted after that date{' '}
                <span className="font-medium text-brand-600">[2]</span>.
              </p>

              <ul className="mt-3 flex flex-col gap-2 border-t border-slate-100 pt-3">
                <li className="rounded-lg bg-slate-50 p-2 text-xs text-slate-600">
                  <span className="font-medium text-slate-700">[1] University Handbook.pdf</span>
                  <span className="ml-2 rounded-full bg-slate-200 px-2 py-0.5 text-slate-600">p. 12</span>
                </li>
                <li className="rounded-lg bg-slate-50 p-2 text-xs text-slate-600">
                  <span className="font-medium text-slate-700">[2] Exam Regulations.pdf</span>
                  <span className="ml-2 rounded-full bg-slate-200 px-2 py-0.5 text-slate-600">p. 24</span>
                </li>
              </ul>
            </div>
          </div>
        </div>
      </section>

      <section id="privacy" className="mx-auto max-w-4xl px-4 py-20 sm:px-6 lg:px-8">
        <div className="relative overflow-hidden rounded-2xl border border-slate-200 bg-white p-8 text-center shadow-sm">
          <span className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-brand-50 text-brand-600">
            <ShieldCheck className="h-6 w-6" aria-hidden="true" />
          </span>
          <h2 className="mt-4 text-2xl">Grounded answers. Verifiable sources. Private by default.</h2>
          <p className="mx-auto mt-4 max-w-2xl text-slate-600">
            DocuMind only answers from the documents you upload, and only you can access your own
            documents and conversations. If the available documents don't contain a reliable answer,
            DocuMind tells you so instead of guessing.
          </p>
        </div>
      </section>
    </main>
  )
}
