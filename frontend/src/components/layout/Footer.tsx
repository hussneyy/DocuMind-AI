export function Footer() {
  return (
    <footer className="border-t border-slate-200 bg-slate-50">
      <div className="mx-auto max-w-6xl px-4 py-10 sm:px-6 lg:px-8">
        <div className="flex flex-col items-start justify-between gap-6 md:flex-row md:items-center">
          <div>
            <p className="text-sm font-semibold text-slate-900">DocuMind AI</p>
            <p className="mt-1 max-w-sm text-sm text-slate-500">
              An open-source portfolio project demonstrating retrieval-augmented document search
              and question answering. Not affiliated with any commercial product.
            </p>
          </div>

          <div className="flex flex-wrap gap-x-8 gap-y-2 text-sm text-slate-500">
            <span className="cursor-default text-slate-400" title="See the project README once published">
              Documentation
            </span>
            <span className="cursor-default text-slate-400" title="See the project README once published">
              Source code
            </span>
            <a href="#privacy" className="hover:text-slate-900">
              Privacy &amp; source verification
            </a>
          </div>
        </div>

        <p className="mt-8 text-xs text-slate-400">
          &copy; {new Date().getFullYear()} DocuMind AI. Built for demonstration purposes.
        </p>
      </div>
    </footer>
  )
}
