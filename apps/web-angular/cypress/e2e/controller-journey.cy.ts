describe('operations controller journey',()=>{
  const signIn=()=>{cy.visit('/login');cy.get('input[formcontrolname="controllerId"]').clear().type('maya.chen');cy.get('input[formcontrolname="password"]').clear().type('operations');cy.contains('button','Sign in').click();cy.url().should('include','/overview')};
  it('protects operational routes and authenticates the controller',()=>{cy.visit('/flights');cy.url().should('include','/login');cy.contains('Welcome back');cy.get('input[formcontrolname="controllerId"]').clear().type('maya.chen');cy.get('input[formcontrolname="password"]').clear().type('operations');cy.contains('button','Sign in').click();cy.url().should('include','/flights');cy.contains('h1','Flights')});
  it('searches flights and opens flight details',()=>{signIn();cy.visit('/flights');cy.get('input[placeholder*="Search flight"]').type('AC103');cy.get('tbody tr').should('have.length',1).click();cy.url().should('include','/flights/AC103');cy.contains('Disruption risk');cy.contains('Aircraft rotation')});
  it('opens airport and aircraft operational details',()=>{signIn();cy.visit('/airports');cy.contains('article','Toronto').click();cy.url().should('include','/airports/YYZ');cy.contains('Weather conditions');cy.visit('/aircraft');cy.contains('article','C-FVLX').click();cy.url().should('include','/aircraft/C-FVLX');cy.contains('Today’s aircraft rotation')});
  it('filters events and follows an affected entity',()=>{signIn();cy.visit('/event-timeline');cy.get('select').first().select('Critical');cy.contains('Weather risk raised').click();cy.url().should('include','/airports/YYZ')});
  it('rejects an option and completes the recovery approval workflow',()=>{
    cy.visit('/login',{onBeforeLoad:win=>win.localStorage.clear()});
    cy.get('input[formcontrolname="controllerId"]').clear().type('maya.chen');
    cy.get('input[formcontrolname="password"]').clear().type('operations');
    cy.contains('button','Sign in').click();
    cy.url().should('include','/overview');
    cy.visit('/recovery-plans');
    cy.contains('.list > article','Severe weather').click();
    cy.url().should('include','/recovery-plans/DSP-001');
    cy.contains('h2','Reassign to compatible gate').should('be.visible');
    cy.contains('.plans article','Maintain current rotation').within(()=>cy.get('[data-cy="reject-plan"]').click());
    cy.get('[data-cy="decision-notes"]').type('Operational risk exceeds the preferred threshold.');
    cy.get('[data-cy="confirm-rejection"]').click();
    cy.contains('.plans article','Rejected').should('be.visible');
    cy.contains('.plans article','Reassign to compatible gate').within(()=>cy.get('[data-cy="approve-plan"]').click());
    cy.get('[data-cy="decision-notes"]').type('Clear the gate conflict and protect downstream passengers.');
    cy.get('body').then($body=>{if($body.find('.supervisor input').length)cy.get('.supervisor input').check()});
    cy.get('[data-cy="confirm-approval"]').click();
    cy.get('[data-cy="recovery-outcome"]').should('contain','Measured network outcome').and('contain','saved');
    cy.visit('/recovery-plans');
    cy.contains('.decision-log','Recovery decision log').should('contain','Approved').and('contain','Rejected');
  });
});
