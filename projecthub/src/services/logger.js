const LOG_ENDPOINT = '/api/logs'
const MAX_QUEUE = 100

class RemoteLogger {
  constructor() {
    this.queue = []
    this.flushTimer = null
    this.originalConsole = null
    this.installed = false
  }

  install() {
    if (this.installed || !import.meta.env.DEV) return
    this.installed = true

    this.originalConsole = {
      log: console.log.bind(console),
      info: console.info.bind(console),
      warn: console.warn.bind(console),
      error: console.error.bind(console)
    }

    console.log = (...args) => {
      this.originalConsole.log(...args)
      this.enqueue('frontend', 'info', this.format(args))
    }
    console.info = (...args) => {
      this.originalConsole.info(...args)
      this.enqueue('frontend', 'info', this.format(args))
    }
    console.warn = (...args) => {
      this.originalConsole.warn(...args)
      this.enqueue('frontend', 'warning', this.format(args))
    }
    console.error = (...args) => {
      this.originalConsole.error(...args)
      this.enqueue('frontend', 'error', this.format(args))
    }

    window.addEventListener('error', event => {
      this.enqueue('frontend', 'error', event.message || 'Window error', {
        filename: event.filename,
        lineno: event.lineno,
        colno: event.colno
      })
    })

    window.addEventListener('unhandledrejection', event => {
      this.enqueue('frontend', 'error', 'Unhandled promise rejection', {
        reason: this.format([event.reason])
      })
    })
  }

  format(args) {
    return args.map(arg => {
      if (typeof arg === 'string') return arg
      try {
        return JSON.stringify(arg)
      } catch {
        return String(arg)
      }
    }).join(' ')
  }

  enqueue(source, level, message, data = null) {
    this.queue.push({ source, level, message, data })
    if (this.queue.length > MAX_QUEUE) this.queue.shift()
    this.scheduleFlush()
  }

  scheduleFlush() {
    if (this.flushTimer) return
    this.flushTimer = setTimeout(() => {
      this.flushTimer = null
      void this.flush()
    }, 1000)
  }

  async flush() {
    const batch = this.queue.splice(0, this.queue.length)
    if (!batch.length || !import.meta.env.DEV) return
    for (const item of batch) {
      try {
        await fetch(LOG_ENDPOINT, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(item)
        })
      } catch {
        // 开发环境日志上报失败不影响业务
      }
    }
  }
}

export const remoteLogger = new RemoteLogger()
