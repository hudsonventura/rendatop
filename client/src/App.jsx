import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { HeroUIProvider } from "@heroui/react"
import './App.css'
import { ThemeProvider } from '@/components/theme-provider'
import { SidebarConfigProvider } from '@/contexts/sidebar-context'
import { WalletProvider } from '@/contexts/wallet-context'
import { ROUTER_BASENAME } from '@/utils/appPath'

import Login from "@/pages/Login"
import Signup from "@/pages/Signup"
import Home from "@/pages/Home"
import Logout from "@/pages/Logout"
import CalendarPage from "@/pages/CalendarPage"
import UserSettings from "@/pages/UserSettings"
import MyInvestments from "@/pages/MyInvestments"
import RecurringInvestmentsPage from "@/pages/RecurringInvestmentsPage"
import NotificationsPage from "@/pages/NotificationsPage"
import ResetPassword from "@/pages/ResetPassword"
import ForgotPassword from "@/pages/ForgotPassword"
import ForgotTotp from "@/pages/ForgotTotp"
import ResetTotp from "@/pages/ResetTotp"
import NotFound from "@/pages/NotFound"
import SubscriptionPage from "@/pages/SubscriptionPage"
import MoneyBoxesPage from "@/pages/MoneyBoxesPage"
import AdminPage from "@/pages/AdminPage"
import SupportPage from "@/pages/SupportPage"
import BlogAdminPage from "@/pages/BlogAdminPage"


function App() {
    return (
        <div className="font-sans antialiased" style={{ fontFamily: 'var(--font-inter)' }}>
            <HeroUIProvider locale="pt-BR">
                <ThemeProvider defaultTheme="system" storageKey="vite-ui-theme">
                    <SidebarConfigProvider>
                        <WalletProvider>
                            <Router basename={ROUTER_BASENAME}>
                                <Routes>
                                <Route path="/" element={<Login />} />
                                <Route path="/login" element={<Login />} />
                                <Route path="/signup" element={<Signup />} />
                                <Route path="/logout" element={<Logout />} />
                                <Route path="/home" element={<Home />} />
                                <Route path="/meus-investimentos" element={<MyInvestments />} />
                                <Route path="/investimentos-recorrentes" element={<RecurringInvestmentsPage />} />
                                <Route path="/calendar" element={<CalendarPage />} />
                                <Route path="/notifications" element={<NotificationsPage />} />
                                <Route path="/cofrinhos" element={<MoneyBoxesPage />} />
                                <Route path="/settings" element={<UserSettings />} />
                                <Route path="/forgot-password" element={<ForgotPassword />} />
                                <Route path="/reset-password" element={<ResetPassword />} />
                                <Route path="/forgot-totp" element={<ForgotTotp />} />
                                <Route path="/reset-totp" element={<ResetTotp />} />
                                <Route path="/subscription" element={<SubscriptionPage />} />
                                <Route path="/admin" element={<AdminPage />} />
                                <Route path="/admin/blog" element={<BlogAdminPage />} />
                                <Route path="/atendimento" element={<SupportPage />} />
                                <Route path="*" element={<NotFound />} />
                                </Routes>
                            </Router>
                        </WalletProvider>
                    </SidebarConfigProvider>
                </ThemeProvider>
            </HeroUIProvider>
        </div>
    );
}

export default App
