describe('operations controller journey',()=>{
  const signIn=()=>{cy.visit('/login');cy.get('input[formcontrolname="controllerId"]').clear().type('maya.chen');cy.get('input[formcontrolname="password"]').clear().type('operations');cy.contains('button','Sign in').click();cy.url().should('include','/overview')};
  it('protects operational routes and authenticates the controller',()=>{cy.visit('/flights');cy.url().should('include','/login');cy.contains('Welcome back');cy.get('input[formcontrolname="controllerId"]').clear().type('maya.chen');cy.get('input[formcontrolname="password"]').clear().type('operations');cy.contains('button','Sign in').click();cy.url().should('include','/flights');cy.contains('h1','Flights')});
  it('signs the controller out and restores route protection',()=>{signIn();cy.get('button[aria-label="Sign out"]').click();cy.url().should('include','/login');cy.visit('/aircraft');cy.url().should('include','/login')});
  it('searches flights and follows aircraft, disruption, and recovery actions',()=>{signIn();cy.visit('/flights');cy.get('input[placeholder*="Search flight"]').type('AC103');cy.get('tbody tr').should('have.length',1).click();cy.url().should('include','/flights/AC103');cy.contains('Disruption risk');cy.contains('button','View aircraft').click();cy.url().should('include','/aircraft/C-FVLX');cy.visit('/flights/AC103');cy.contains('button','Open disruption record').click();cy.url().should('include','/disruptions/DSP-001');cy.visit('/flights/AC103');cy.contains('button','Evaluate recovery options').click();cy.url().should('include','/recovery-plans/DSP-001');cy.contains('Recovery options for AC103')});
  it('opens airport and aircraft operational details',()=>{signIn();cy.visit('/airports');cy.contains('article','Toronto').click();cy.url().should('include','/airports/YYZ');cy.contains('Weather conditions');cy.visit('/aircraft');cy.contains('article','C-FVLX').click();cy.url().should('include','/aircraft/C-FVLX');cy.contains('Today’s aircraft rotation')});
  it('filters events and follows an affected entity',()=>{signIn();cy.visit('/event-timeline');cy.get('select').first().select('Critical');cy.contains('Weather risk raised').click();cy.url().should('include','/airports/YYZ')});
  it('filters and clears flight, airport, aircraft, and disruption collections',()=>{
    signIn();
    cy.visit('/flights');
    cy.get('input[placeholder*="Search flight"]').type('NO-SUCH-FLIGHT');
    cy.contains('No flights match these filters').should('be.visible');
    cy.contains('button','Clear filters').click();
    cy.get('tbody tr').should('have.length.greaterThan',0);
    cy.visit('/airports');
    cy.get('input[placeholder*="Search airport"]').type('NO-SUCH-AIRPORT');
    cy.contains('No airports found').should('be.visible');
    cy.contains('button','Clear filters').click();
    cy.contains('article','Toronto').should('be.visible');
    cy.visit('/aircraft');
    cy.get('input[placeholder*="Search registration"]').type('NO-SUCH-AIRCRAFT');
    cy.contains('No aircraft match').should('be.visible');
    cy.contains('button','Clear filters').click();
    cy.contains('article','C-FVLX').should('be.visible');
    cy.visit('/disruptions');
    cy.contains('button','Resolved').click();
    cy.contains('.list article','Resolved').should('be.visible');
    cy.contains('button','All').click();
    cy.contains('.list article','DSP-001').should('be.visible');
  });
  it('connects every overview call to action to its operational workspace',()=>{
    signIn();
    cy.get('button[aria-label="Notifications"]').click();
    cy.url().should('include','/event-timeline');
    cy.visit('/overview');
    cy.contains('button','Run scenario').click();
    cy.url().should('include','/disruptions/scenarios');
    cy.visit('/overview');
    cy.get('[aria-label="Open YYZ airport operations"]').click();
    cy.url().should('include','/airports/YYZ');
    cy.visit('/overview');
    cy.contains('button','View affected flights').click();
    cy.url().should('include','/flights?search=YYZ');
    cy.get('input[placeholder*="Search flight"]').should('have.value','YYZ');
    cy.get('tbody tr').should('have.length.greaterThan',0);
  });
  it('prefills disruption workflows from airport and aircraft records',()=>{
    signIn();
    cy.visit('/airports/YYZ');
    cy.contains('button','Open disruption workspace').click();
    cy.url().should('include','/disruptions?');
    cy.contains('h2','Trigger a disruption').should('be.visible');
    cy.get('select[formcontrolname="type"]').should('have.value','Airport congestion');
    cy.get('select[formcontrolname="airport"]').should('have.value','YYZ');
    cy.contains('button','×').click();
    cy.visit('/aircraft/C-FVLX');
    cy.contains('button','Open maintenance disruption').click();
    cy.contains('h2','Trigger a disruption').should('be.visible');
    cy.get('select[formcontrolname="type"]').should('have.value','Aircraft maintenance');
    cy.get('select[formcontrolname="airport"]').should('have.value','YYZ');
    cy.get('select[formcontrolname="flightId"]').should('have.value','AC103');
    cy.visit('/aircraft/C-GJYE');
    cy.contains('Aircraft unavailable pending technical release').should('be.visible');
    cy.contains('Technical release pending').should('be.visible');
    cy.contains('button','Open maintenance disruption').click();
    cy.get('select[formcontrolname="airport"]').should('have.value','YUL');
    cy.get('select[formcontrolname="flightId"]').should('have.value','AC791');
  });
  it('generates recovery plans directly from the recovery queue',()=>{
    signIn();
    cy.request('POST','/api/disruptions',{
      type:'Crew timing issue',severity:'Moderate',airport:'YYC',flightId:'AC156',durationMinutes:45,
    }).then(response=>{
      const id=(response.body as {id:string}).id;
      cy.visit('/recovery-plans');
      cy.contains('.list article',id).within(()=>cy.contains('button','Generate plans').click());
      cy.url().should('include',`/recovery-plans/${id}`);
      cy.contains('h2','Maintain current rotation').should('be.visible');
      cy.contains('Backend recommendation engine').should('be.visible');
    });
  });
  it('resolves an active disruption and records the state change',()=>{
    signIn();
    cy.request('POST','/api/disruptions',{
      type:'Airport congestion',severity:'Moderate',airport:'YVR',flightId:'AC103',durationMinutes:30,
    }).then(response=>{
      const id=(response.body as {id:string}).id;
      cy.visit(`/disruptions/${id}`);
      cy.contains('h1','Airport congestion').should('be.visible');
      cy.contains('button','Mark resolved').click();
      cy.url().should('match',/\/disruptions$/);
      cy.contains('button','Resolved').click();
      cy.contains('.list article',id).should('contain','Resolved');
    });
  });
  it('runs and replays scenario controls without dead actions',()=>{
    signIn();
    cy.visit('/disruptions/scenarios');
    cy.contains('article','Calgary Crew Shortage').within(()=>cy.contains('button','Run scenario').click());
    cy.contains('.runs article','Calgary Crew Shortage').should('be.visible');
    cy.contains('button','Replay with same inputs').click();
    cy.contains('.runs article','Replay').should('be.visible');
    cy.contains('button','Reset simulation').click();
    cy.contains('button','Cancel').click();
    cy.contains('Reset the simulation?').should('not.exist');
    cy.contains('button','Run network stress test').click();
    cy.get('.runs article').should('have.length',3);
    cy.contains('button','Reset simulation').click();
    cy.contains('button','Reset everything').click();
    cy.contains('No scenarios run yet').should('be.visible');
  });
  it('rejects an option and completes the recovery approval workflow',()=>{
    cy.visit('/login',{onBeforeLoad:win=>win.localStorage.clear()});
    cy.get('input[formcontrolname="controllerId"]').clear().type('maya.chen');
    cy.get('input[formcontrolname="password"]').clear().type('operations');
    cy.contains('button','Sign in').click();
    cy.url().should('include','/overview');
    cy.visit('/disruptions');
    cy.contains('button','Trigger disruption').click();
    cy.get('select[formcontrolname="type"]').select('Gate conflict');
    cy.get('select[formcontrolname="severity"]').select('Moderate');
    cy.get('select[formcontrolname="airport"]').select('YYC');
    cy.get('select[formcontrolname="flightId"]').select('AC156');
    cy.get('input[formcontrolname="durationMinutes"]').clear().type('30');
    cy.contains('button','Trigger and calculate impact').click();
    cy.url().should('match',/\/disruptions\/DSP-\d+$/);
    cy.contains('button','Generate recovery plans').click();
    cy.url().should('match',/\/recovery-plans\/DSP-\d+$/);
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
