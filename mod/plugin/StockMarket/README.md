# Stock Market & Hostile Takeover System

**GDD Point 2** - Complete stock market simulation with tiered ownership and hostile takeovers.

## Features

### 1. Stock Ownership Tiers
- **5% - Dividend Rights**: Receive quarterly dividends from company revenue
- **25% - Board Seat**: Get notified when they start developing new games
- **51% - Controlling Interest**: Influence their strategic decisions
- **100% - Full Acquisition**: Complete merger into your company as subsidiary

### 2. Company Valuation
- Based on company worth (firmenwert) + recent revenue
- Bonus for awards and successful games
- Discount for struggling companies

### 3. Hostile Takeover & Defense
- AI companies can activate "Poison Pill" defense
- Makes further acquisitions 50% more expensive for 1 year
- Triggered when you acquire 25%+ of a successful company

### 4. Quarterly Dividends
- Automatic payout every 3 months
- 10% of quarterly revenue × your ownership %
- Notification with total dividends received

### 5. Board Intelligence
- 25%+ ownership = see their secret projects early
- Get notified when they start developing games
- Strategic advantage for market timing

## UI/UX

### Stock Market Window
- **Hotkey**: F8
- **Button**: Top-right corner of main menu (visible in-game)
- **Layout**: Vertical scrollable list, similar to game's style
  - Company logo (64x64)
  - Company name and market value
  - Your current stake (% and tier name)
  - Buy/Sell/Info buttons

### Buy Shares Dialog
- Shows all available tiers with descriptions
- Real-time price calculation
- Color-coded buttons (green = buy)
- Tier benefits clearly explained

### Sell Shares Dialog
- Options: 25%, 50%, or All
- Shows sale value in green (+$X.XM)
- Orange/red buttons (caution)

## Installation

1. Copy `StockMarket.dll` to `BepInEx\plugins\`
2. Start game - mod loads automatically
3. Press F8 to open Stock Market window

## Technical Details

### Save System
- Stored in `{savename}{slot}.stockmarket.json`
- Persists across game sessions
- Tracks ownership for all companies

### Weekly Mechanics
- Dividend calculations
- Poison pill expiry checks
- Board intelligence notifications

### Integration
- Works with existing company buy/sell system
- 100% acquisition converts to subsidiary (Tochterfirma)
- Compatible with multiplayer (disabled in MP)

## Testing Checklist

- [ ] Open stock market window (F8)
- [ ] Buy 5% shares - receive dividend notification
- [ ] Buy 25% shares - see board intelligence
- [ ] Buy 51% shares - get controlling interest message
- [ ] Buy 100% - company becomes subsidiary
- [ ] Sell shares - receive money
- [ ] Wait 3 months - quarterly dividend payout
- [ ] Hostile takeover triggers poison pill
- [ ] Save/Load game - ownership persists

## Files Modified/Created

### Plugin Files
- `mod/plugin/StockMarket/StockMarket.cs` - Core plugin & data structures
- `mod/plugin/StockMarket/StockMarketWindow.cs` - Main UI window
- `mod/plugin/StockMarket/ShareTransactions.cs` - Buy/sell dialogs
- `mod/plugin/StockMarket/AdvancedMechanics.cs` - Dividends, hostile takeover, board seat
- `mod/plugin/StockMarket/ManagementIntegration.cs` - F8 hotkey & button
- `mod/plugin/StockMarket/StockMarket.csproj` - Build configuration

### Output
- `mod/plugin/StockMarket/bin/Release/netstandard2.1/StockMarket.dll`

## Known Limitations

- Poison pill price increase: 50% (fixed multiplier)
- Board intelligence: notification only (no detailed stats yet)
- Controlling interest: subtle influence (not full control of their dev)
- UI: English text only (Arabic translation pending)

## Future Enhancements (GDD)

- IPO system: player can go public and sell own company shares
- AI shareholders: competitors buy each other's shares
- Stock market crashes and booms
- Leverage and hostile takeover loans
- Shareholder voting on major decisions
