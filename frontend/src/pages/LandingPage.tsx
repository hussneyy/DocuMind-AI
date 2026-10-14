import { Button } from '../components/ui/Button'

const FEATURES = [
  {
    title: 'Semantic search',
    description:
      'Find relevant passages across your PDFs by meaning, not just keywords, using vector similarity search.',
  },
  {
    title: 'Grounded AI answers',
    description:
      'Every answer is generated from retrieved document passages, never from unsupported general knowledge.',
  },
  {
    title: 'Verifiable citations',
    description:
      'Click any citation to jump straight to the source document and page that supports the answer.',
  },
  {
    title: 'Document summaries',
    description:
      'Generate short, detailed, or section-by-section summaries of long documents on demand.',
  },
  {
    title: 'Private by default',
    description:
      'Your documents are only ever visible to you. Ownership is enforced on every request, every time.',
  },
  {
    title: 'Built for teams and research',
    description:
      'Organize handbooks, papers, and policies in one searchable library your whole workflow can rely on.',
  },
]

const STEPS = [
  {
    number: '01',
    title: 'Upload',
    description: 'Drag in one or more PDF documents. DocuMind extracts and indexes the text, page by page.',
  },
  {
    number: '02',
    title: 'Ask',
    description: 'Ask a question in plain language, scoped to one document, several, or your whole library.',
  },
  {
    number: '03',
    title: 'Discover',
    description: 'Get a grounded answer with citations you can click to verify against the original source.',
  },
]

export function LandingPage() {
  return (
    <main>
      <section className="relative overflow-hidden bg-gradient-to-b from-brand-50 to-white">
        <div className="mx-auto max-w-6xl px-4 py-20 sm:px-6 sm:py-28 lg:px-8">
          <div className="mx-auto max-w-3xl text-center">
            <p className="inline-flex items-center rounded-full bg-brand-100 px-3 py-1 text-xs font-semibold text-brand-700">
              Portfolio project &middot; retrieval-augmented document QA
            </p>
            <h1 className="mt-6 text-4xl sm:text-5xl">
              Ask your documents anything. Get answers you can verify.
            </h1>
            <p className="mt-6 text-lg text-slate-600">
              DocuMind AI turns your PDFs into a searchable knowledge base. Upload handbooks, papers,
              or policies, ask natural-language questions, and get grounded answers with real source
              citations &mdash; never invented page numbers or quotes.
            </p>
            <div className="mt-10 flex flex-col items-center justify-center gap-3 sm:flex-row">
              <Button to="/register" size="lg">
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
          {STEPS.map((step) => (
            <div key={step.number} className="relative rounded-2xl border border-slate-200 p-6">
              <span className="text-sm font-semibold text-brand-600">{step.number}</span>
              <h3 className="mt-2 text-xl">{step.title}</h3>
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
            {FEATURES.map((feature) => (
              <div key={feature.title} className="rounded-2xl bg-white p-6 shadow-sm ring-1 ring-slate-200">
                <h3 className="text-lg">{feature.title}</h3>
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
          <div className="rounded-xl bg-slate-50 p-4">
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-400">Question</p>
            <p className="mt-1 text-slate-800">"When is the final exam registration deadline?"</p>
          </div>

          <div className="mt-4 rounded-xl border border-brand-100 bg-brand-50 p-4">
            <p className="text-xs font-semibold uppercase tracking-wide text-brand-600">Answer</p>
            <p className="mt-1 text-slate-800">
              "The handbook states that exam registration closes two weeks before the exam period
              begins."
            </p>

            <p className="mt-4 text-xs font-semibold uppercase tracking-wide text-brand-600">
              Sources
            </p>
            <ul className="mt-1 space-y-1 text-sm text-slate-700">
              <li className="flex items-center gap-2">
                <span className="h-1.5 w-1.5 rounded-full bg-brand-500" aria-hidden="true" />
                University Handbook.pdf &mdash; Page 12
              </li>
              <li className="flex items-center gap-2">
                <span className="h-1.5 w-1.5 rounded-full bg-brand-500" aria-hidden="true" />
                Exam Regulations.pdf &mdash; Page 24
              </li>
            </ul>
          </div>
        </div>
      </section>

      <section id="privacy" className="mx-auto max-w-4xl px-4 py-20 sm:px-6 lg:px-8">
        <div className="rounded-2xl border border-slate-200 bg-white p-8 text-center shadow-sm">
          <h2 className="text-2xl">Grounded answers. Verifiable sources. Private by default.</h2>
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
