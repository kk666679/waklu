import ora from 'ora'

/**
 * Run `fn` behind a spinner, succeeding or failing with the error message.
 *
 * The spinner is always stopped before the error propagates, so a thrown
 * service error cannot leave a stray `⠋` on the terminal while Commander
 * prints its own failure line.
 */
export async function withSpinner<T>(text: string, fn: () => Promise<T>): Promise<T> {
  const spinner = ora(text).start()
  try {
    const result = await fn()
    spinner.succeed()
    return result
  } catch (err) {
    spinner.fail((err as Error).message)
    throw err
  }
}
