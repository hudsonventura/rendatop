using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Shapes;
using RendaTop.App.Services;

namespace RendaTop.App;

public partial class AppShell : Shell
{
	private readonly IServiceProvider _services;
	private readonly WalletService _wallets;
	private readonly Picker _walletPicker;
	private readonly Button _createWalletButton;
	private bool _walletPickerUpdating;

	public AppShell(IServiceProvider services)
	{
		_services = services;
		_wallets = services.GetRequiredService<WalletService>();
		_walletPicker = CreateWalletPicker();
		_createWalletButton = CreateWalletButton();
		InitializeComponent();

		FlyoutHeader = CreateFlyoutHeader();
		FlyoutFooter = CreateFlyoutFooter();
		PropertyChanged += async (_, args) =>
		{
			if (args.PropertyName == nameof(FlyoutIsPresented) && FlyoutIsPresented)
				await LoadWalletsAsync();
		};

		Items.Add(CreateShellContent<Pages.SplashPage>("splash", "Splash", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.WelcomePage>("welcome", "Boas-vindas", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.LoginPage>("login", "Entrar", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.SignupPage>("signup", "Criar conta", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.AddInvestmentPage>("add-investment", "Novo Investimento", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.InvestmentDetailsPage>("investment-details", "Detalhes do Investimento", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.RedeemInvestmentPage>("redeem-investment", "Criar Resgate", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.EditRedemptionPage>("edit-redemption", "Editar Resgate", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.EditRecurringInvestmentPage>("edit-recurring-investment", "Recorrencia", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.EditMoneyBoxPage>("edit-money-box", "Cofrinho", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.CalendarDayEventsPage>("calendar-day-events", "Eventos do Dia", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.CalendarEventDetailsPage>("calendar-event-details", "Detalhes do Evento", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.SubscriptionCheckoutPage>("subscription-checkout", "Pagamento", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.CreateSupportTicketPage>("create-support-ticket", "Novo Chamado", showInFlyout: false));
		Items.Add(CreateShellContent<Pages.SupportTicketDetailsPage>("support-ticket-details", "Detalhes do Chamado", showInFlyout: false));
		Items.Add(CreateFlyoutItem<Pages.DashboardPage>("dashboard", "Dashboard"));
		Items.Add(CreateFlyoutItem<Pages.CalendarPage>("calendar", "Calendario"));
		Items.Add(CreateFlyoutItem<Pages.MyInvestmentsPage>("meus-investimentos", "Meus Investimentos"));
		Items.Add(CreateFlyoutItem<Pages.NotificationsPage>("notifications", "Notificacoes"));
		Items.Add(CreateFlyoutItem<Pages.RecurringInvestmentsPage>("investimentos-recorrentes", "Investimentos Recorrentes"));
		Items.Add(CreateFlyoutItem<Pages.MoneyBoxesPage>("cofrinhos", "Cofrinhos"));
		Items.Add(CreateFlyoutItem<Pages.SettingsPage>("settings", "Configuracoes"));
		Items.Add(CreateFlyoutItem<Pages.SubscriptionPage>("subscription", "Assinatura"));
		Items.Add(CreateFlyoutItem<Pages.SupportPage>("atendimento", "Atendimento"));

		Routing.RegisterRoute(nameof(Pages.SignupVerificationPage), typeof(Pages.SignupVerificationPage));
		Routing.RegisterRoute(nameof(Pages.LoginPage), typeof(Pages.LoginPage));
		Routing.RegisterRoute(nameof(Pages.SignupPage), typeof(Pages.SignupPage));
		Routing.RegisterRoute(nameof(Pages.AddInvestmentPage), typeof(Pages.AddInvestmentPage));
		Routing.RegisterRoute(nameof(Pages.InvestmentDetailsPage), typeof(Pages.InvestmentDetailsPage));
		Routing.RegisterRoute(nameof(Pages.RedeemInvestmentPage), typeof(Pages.RedeemInvestmentPage));
		Routing.RegisterRoute(nameof(Pages.EditRedemptionPage), typeof(Pages.EditRedemptionPage));
		Routing.RegisterRoute(nameof(Pages.EditRecurringInvestmentPage), typeof(Pages.EditRecurringInvestmentPage));
		Routing.RegisterRoute(nameof(Pages.EditMoneyBoxPage), typeof(Pages.EditMoneyBoxPage));
		Routing.RegisterRoute(nameof(Pages.CalendarDayEventsPage), typeof(Pages.CalendarDayEventsPage));
		Routing.RegisterRoute(nameof(Pages.CalendarEventDetailsPage), typeof(Pages.CalendarEventDetailsPage));
		Routing.RegisterRoute(nameof(Pages.SubscriptionCheckoutPage), typeof(Pages.SubscriptionCheckoutPage));
		Routing.RegisterRoute(nameof(Pages.CreateSupportTicketPage), typeof(Pages.CreateSupportTicketPage));
		Routing.RegisterRoute(nameof(Pages.SupportTicketDetailsPage), typeof(Pages.SupportTicketDetailsPage));
	}

	private ShellContent CreateShellContent<TPage>(string route, string title, bool showInFlyout)
		where TPage : Page
	{
		var content = new ShellContent
		{
			Route = route,
			Title = title,
			ContentTemplate = new DataTemplate(() => _services.GetRequiredService<TPage>())
		};

		Shell.SetNavBarIsVisible(content, showInFlyout);
		Shell.SetFlyoutItemIsVisible(content, showInFlyout);
		Shell.SetFlyoutBehavior(content, showInFlyout ? FlyoutBehavior.Flyout : FlyoutBehavior.Disabled);
		return content;
	}

	private FlyoutItem CreateFlyoutItem<TPage>(string route, string title)
		where TPage : Page
	{
		var item = new FlyoutItem
		{
			Route = route,
			Title = title,
			FlyoutDisplayOptions = FlyoutDisplayOptions.AsSingleItem,
			Items =
			{
				new ShellContent
				{
					Route = $"{route}-content",
					Title = title,
					ContentTemplate = new DataTemplate(() => _services.GetRequiredService<TPage>())
				}
			}
		};

		return item;
	}

	private FlyoutItem CreateFutureFlyoutItem(string route, string title, string description)
	{
		return new FlyoutItem
		{
			Route = route,
			Title = title,
			FlyoutDisplayOptions = FlyoutDisplayOptions.AsSingleItem,
			Items =
			{
				new ShellContent
				{
					Route = $"{route}-content",
					Title = title,
					ContentTemplate = new DataTemplate(() => new Pages.FuturePage(title, description))
				}
			}
		};
	}

	private View CreateFlyoutHeader()
	{
		return new Grid
		{
			Padding = new Thickness(20, 24, 20, 14),
			BackgroundColor = Color.FromArgb("#111827"),
			Children =
			{
				new VerticalStackLayout
				{
					Spacing = 10,
					Children =
					{
						new Image
						{
							Source = "brand_icon.svg",
							HeightRequest = 48,
							WidthRequest = 48,
							HorizontalOptions = LayoutOptions.Start
						},
						new Label
						{
							Text = "RendaTop",
							FontSize = 22,
							FontAttributes = FontAttributes.Bold,
							TextColor = Colors.White
						},
						new Label
						{
							Text = "Gestao de investimentos",
							FontSize = 13,
							TextColor = Color.FromArgb("#CBD5E1")
						},
						CreateWalletPickerContainer()
					}
				}
			}
		};
	}

	private View CreateWalletPickerContainer()
	{
		return new Border
		{
			BackgroundColor = Color.FromArgb("#1E293B"),
			Stroke = Color.FromArgb("#334155"),
			StrokeThickness = 1,
			Margin = new Thickness(0, 4, 0, 0),
			Padding = new Thickness(10, 2, 4, 2),
			Content = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Auto)
				},
				Children =
				{
					_walletPicker,
					_createWalletButton
				}
			},
			StrokeShape = new RoundRectangle
			{
				CornerRadius = 12
			}
		};
	}

	private Picker CreateWalletPicker()
	{
		var picker = new Picker
		{
			Title = "Carteira",
			HeightRequest = 42,
			FontSize = 14,
			TextColor = Colors.White,
			TitleColor = Color.FromArgb("#CBD5E1"),
			BackgroundColor = Colors.Transparent,
			Margin = new Thickness(0),
			HorizontalOptions = LayoutOptions.Fill,
			ItemDisplayBinding = new Binding(nameof(ShellWalletItem.Name)),
			IsVisible = false
		};

		picker.SelectedIndexChanged += OnWalletSelected;
		return picker;
	}

	private Button CreateWalletButton()
	{
		var button = new Button
		{
			Text = "+",
			FontSize = 24,
			FontAttributes = FontAttributes.Bold,
			TextColor = Colors.White,
			BackgroundColor = Color.FromArgb("#334155"),
			WidthRequest = 38,
			HeightRequest = 38,
			Padding = 0,
			CornerRadius = 8,
			Margin = new Thickness(4, 0, 0, 0),
			VerticalOptions = LayoutOptions.Center,
			HorizontalOptions = LayoutOptions.End
		};

		Grid.SetColumn(button, 1);
		SemanticProperties.SetDescription(button, "Nova carteira");
		button.Clicked += OnCreateWalletClicked;
		return button;
	}

	private async Task LoadWalletsAsync()
	{
		try
		{
			var overview = await _wallets.GetOverviewAsync();
			var enabled = (overview.Items ?? [])
				.Where(item => item.Enabled)
				.Select(item => new ShellWalletItem(item.Id, item.Name))
				.ToList();

			_walletPickerUpdating = true;
			_walletPicker.ItemsSource = enabled;

			var activeId = _wallets.ActiveWalletId;
			var activeIndex = activeId.HasValue
				? enabled.FindIndex(item => item.Id == activeId.Value)
				: -1;
			_walletPicker.SelectedIndex = activeIndex >= 0 ? activeIndex : enabled.Count > 0 ? 0 : -1;
			_walletPicker.IsVisible = enabled.Count > 0;
		}
		catch
		{
			_walletPicker.IsVisible = false;
		}
		finally
		{
			_walletPickerUpdating = false;
		}
	}

	private void OnWalletSelected(object? sender, EventArgs e)
	{
		if (_walletPickerUpdating || _walletPicker.SelectedItem is not ShellWalletItem item)
			return;

		_wallets.SetActiveWallet(item.Id);
	}

	private async void OnCreateWalletClicked(object? sender, EventArgs e)
		=> await CreateWalletAsync();

	private async Task CreateWalletAsync()
	{
		_createWalletButton.IsEnabled = false;

		try
		{
			var overview = await _wallets.GetOverviewAsync();
			if (!overview.CanCreate)
			{
				await ShowWalletUpgradeAsync(overview.RestrictionMessage);
				return;
			}

			var name = await DisplayPromptAsync(
				"Nova carteira",
				"Informe um nome para sua nova carteira.",
				"Criar",
				"Cancelar",
				placeholder: "Ex.: Reserva de emergencia",
				maxLength: 80);

			if (name is null)
				return;

			if (string.IsNullOrWhiteSpace(name))
			{
				await DisplayAlertAsync("Nome obrigatorio", "Informe um nome para a carteira.", "OK");
				return;
			}

			var wallet = await _wallets.CreateAsync(name);
			_wallets.SetActiveWallet(wallet.Id);
			await LoadWalletsAsync();
			FlyoutIsPresented = false;
		}
		catch (ApiException ex) when (ex.StatusCode == 403)
		{
			await ShowWalletUpgradeAsync(ex.Message);
		}
		catch (ApiException ex)
		{
			await DisplayAlertAsync("Nao foi possivel criar a carteira", ex.Message, "OK");
		}
		catch
		{
			await DisplayAlertAsync("Nao foi possivel criar a carteira", "Verifique sua conexao e tente novamente.", "OK");
		}
		finally
		{
			_createWalletButton.IsEnabled = true;
		}
	}

	private async Task ShowWalletUpgradeAsync(string? restrictionMessage)
	{
		var message = string.IsNullOrWhiteSpace(restrictionMessage)
			? "Apenas usuarios de planos pagos podem criar mais carteiras e acessar limites estendidos."
			: restrictionMessage;

		var openSubscription = await DisplayAlertAsync(
			"Limite do plano",
			message,
			"Ver planos",
			"Agora nao");

		if (openSubscription)
		{
			FlyoutIsPresented = false;
			await GoToAsync("//subscription");
		}
	}

	private View CreateFlyoutFooter()
	{
		var logout = new Button
		{
			Text = "Sair",
			BackgroundColor = Color.FromArgb("#111827"),
			TextColor = Colors.White,
			FontAttributes = FontAttributes.Bold,
			Margin = new Thickness(16, 8, 16, 18)
		};

		logout.Clicked += async (_, _) =>
		{
			logout.IsEnabled = false;
			logout.Text = "Saindo...";
			await _services.GetRequiredService<AuthService>().LogoutAsync();
			FlyoutIsPresented = false;
			await GoToAsync("//login");
			logout.Text = "Sair";
			logout.IsEnabled = true;
		};

		return logout;
	}
}

public sealed record ShellWalletItem(Guid Id, string Name);
