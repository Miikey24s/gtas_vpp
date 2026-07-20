import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it } from 'vitest'
import { MemoryRouter } from 'react-router-dom'

import { AppProviders } from '@/app/app-providers'
import { LoginPage } from '@/app/routes/login-page'
import { i18n } from '@/lib/i18n'

describe('React authentication entry point', () => {
  beforeEach(async () => {
    window.sessionStorage.clear()
    window.localStorage.clear()
    window.history.replaceState({}, '', '/login')
    await i18n.changeLanguage('vi')
  })

  it('keeps validation space stable and switches to English', async () => {
    const user = userEvent.setup()
    render(
      <MemoryRouter initialEntries={['/login']}>
        <AppProviders>
          <LoginPage />
        </AppProviders>
      </MemoryRouter>,
    )

    expect(
      await screen.findByRole('heading', { name: 'Đăng nhập' }),
    ).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: 'Đăng nhập' }))
    expect(screen.getByText('Vui lòng nhập tên đăng nhập.')).toBeVisible()
    expect(screen.getByText('Vui lòng nhập mật khẩu.')).toBeVisible()

    await user.click(screen.getByRole('button', { name: 'EN' }))
    expect(
      await screen.findByRole('heading', { name: 'Sign in' }),
    ).toBeInTheDocument()
  })
})
